# AI Coding Agents: Kashout Project Playbook

Use these rules to be productive immediately in this codebase. Keep changes focused and aligned with existing patterns.

## Big Picture
- **Architecture**: `.NET 8` Web API (`src/Kashout.Api`) + core domain/services (`src/Kashout.Core`). C# tests in `tests/Kashout.Tests`, optional TypeScript integration tests in `tests/*.ts`.
- **Verification flow**: API receives user/device/docs → runs multiple `IVerificationService` providers **in parallel** via `Task.WhenAll` → `WeightedScorer` aggregates results → `PaymentDecisionEngine` returns allowed methods, limits, and audit trail.
- **MVP status**: Uses fake/simulated provider implementations with realistic data patterns. No real Plaid/Stripe API calls; focused on demonstrating scoring logic and decision engine.
- **Swagger**: Always enabled at root (`/`). API base: `http://localhost:5000`. Controller route base: `/api/v1/verification`.

## Key Files & Their Responsibilities

### API Layer (`src/Kashout.Api/`)
- **Program.cs**: Entry point, listens on `0.0.0.0:5000`, Swagger at `/`, exposes `public partial class Program` for `WebApplicationFactory` testing.
- **Controllers/VerificationController.cs**: All 8 endpoints live here. Constructor instantiates services directly (no DI container in MVP). Parallel verification via `_verificationServices.Select(s => s.VerifyAsync(data))` then `Task.WhenAll`.

### Core Domain (`src/Kashout.Core/`)
- **Models/VerificationModels.cs**: All DTOs, enums, interfaces (`IVerificationService`, `IComplianceChecker`). Contains both domain models and API request/response shapes.
- **Services/ScoringEngine.cs**: 
  - `WeightedScorer`: Holds `_weights` dictionary (provider_key → weight). `CalculateScore()` multiplies each provider's score × weight × confidence, divides by total weight.
  - `PaymentDecisionEngine`: Calls `WeightedScorer`, then maps score ranges to `AllowedMethods` and `TransactionLimits`. Returns full `PaymentDecision` with audit trail.
  - `ComplianceChecker`: Stub implementation validates basic compliance flags.
- **Services/Verification/VerificationServices.cs**: All provider implementations (`PlaidVerificationService`, `StripeVerificationService`, `WebAuthnVerificationService`, `JumioVerificationService`, `AdvancedFraudDetectionService`, `MLRiskScoringService`). Each returns `VerificationResult` with `Provider` string matching a key in `_weights`.
- **Services/TestDataGenerator.cs**, **PlaidFakeDataGenerator.cs**, **PosFakeDataGenerator.cs**: Generate deterministic test scenarios (`HighTrustUser`, `MediumRiskUser`, etc.).

## Provider Pattern (Critical)

### Existing Providers & Weights (as of now)
```csharp
// WeightedScorer._weights
{ "plaid_identity", 0.15 }
{ "stripe_identity", 0.15 }
{ "advanced_fraud_detection", 0.25 }
{ "ml_risk_scoring", 0.30 }
{ "webauthn", 0.10 }
{ "nist_aal", 0.05 }
```
**Note**: `JumioVerificationService` exists but returns `"jumio_risk"` which is NOT in `_weights` → ignored in final score. To enable, add `{ "jumio_risk", <weight> }`.

### Adding a New Provider (3-step recipe)
1. **Create class** in `Services/Verification/VerificationServices.cs` implementing `IVerificationService`. Set `Provider` property to a unique snake_case key (e.g., `"experian_credit"`).
2. **Wire in controller**: Add instance to `_verificationServices` list in `VerificationController` constructor.
3. **Add weight**: Insert entry in `WeightedScorer._weights` with your provider key. Ensure sum of all weights makes sense for your scoring goals (current weights sum to 1.0).
4. **Verify**: Call `POST /api/v1/verification/identity`, check response `Results` array includes your provider, and `Breakdown` in score includes your weighted contribution.

## Scoring & Decision Thresholds

### Score Calculation
```csharp
finalScore = Σ(providerScore × weight × confidence) / Σ(weight × confidence)
```
Each `VerificationResult` has `Score` (0-100), `Confidence` (0.0-1.0), and `Provider` (must match `_weights` key).

### Recommendation Tiers (from `WeightedScorer.GetRecommendation`)
| Score Range | Recommendation | Allowed Methods |
|-------------|----------------|-----------------|
| ≥ 90 | `Approve` | All (CreditCard, DebitCard, BankTransfer, ACH) |
| 75-89 | `ApproveWithLimits` | Cards + BankTransferVerified |
| 60-74 | *(implicit)* | CreditCard, DebitCard only |
| 45-59 | `RequireAdditionalVerification` | CreditCardLimited, DebitCardLimited |
| < 45 | `Decline` | None (or minimal limits) |

### Transaction Limits (from `PaymentDecisionEngine.CalculateLimits`)
| Score | MaxAmount | DailyLimit | MonthlyLimit | MaxTxPerDay |
|-------|-----------|------------|--------------|-------------|
| ≥ 90 | $10,000 | $25,000 | $100,000 | 50 |
| 75-89 | $5,000 | $10,000 | $50,000 | 25 |
| 60-74 | $2,500 | $5,000 | $25,000 | 15 |
| < 60 | $500 | $1,000 | $5,000 | 5 |

**Additional Verification** triggers (score < 75 → phone_verification; score < 60 → add email_verification + document_upload).

## Conventions & Gotchas

- **Provider naming**: Use lowercase snake_case for `VerificationResult.Provider`. Must match keys in `WeightedScorer._weights` exactly or provider is ignored.
- **DTOs**: Controller sometimes defines inline request classes; reuse models from `Kashout.Core.Models` when possible. Avoid duplicating shape definitions.
- **Dependency injection**: MVP uses direct instantiation in controller ctor. No `builder.Services.Add*` calls. If refactoring to DI, update `Program.cs` and controller together.
- **Parallel execution**: All `_verificationServices` run via `Task.WhenAll`. Don't add blocking or sequential calls in provider implementations.
- **Testing hooks**: `public partial class Program` enables `WebApplicationFactory<Program>` in tests. Do not remove `partial` keyword.
- **Port mismatch**: README mentions HTTPS on `:5001`, but `Program.cs` only binds HTTP `:5000`. Source of truth is `Program.cs`.

## Build, Run, Test Workflows

### Quick Start
```bash
dotnet restore && dotnet build
dotnet run --project src/Kashout.Api
# API live at http://localhost:5000 (Swagger at root)
```

### Testing
```bash
# C# unit/integration tests (recommended for CI)
./run-tests.sh
# or directly:
dotnet test tests/Kashout.Tests/Kashout.Tests.csproj

# Full suite (C# + TypeScript integration/pen tests)
./run-all-tests.sh
# Requires: Node 18+, npx tsx, optional Supabase env vars for TS tests
# Phases: 01-functional-tests.ts, 02-security-tests.ts, 03-penetration-tests.ts, test-verification-system.ts
```

### Manual API Testing
```bash
# Generate test data (scenarios: low_risk, medium_risk, high_risk, corporate, fraudulent)
curl -X POST http://localhost:5000/api/v1/verification/test-data \
  -H "Content-Type: application/json" \
  -d '{"scenario":"low_risk"}'

# Run verification (returns all provider results + breakdown)
curl -X POST http://localhost:5000/api/v1/verification/identity \
  -H "Content-Type: application/json" \
  -d @test_request.json

# Get test scenarios
curl http://localhost:5000/api/v1/verification/test-scenarios
```

See `test-api.sh` for more examples with `jq` formatting.

## How to Extend Safely

### Adjusting Weights
1. Edit `WeightedScorer._weights` in `ScoringEngine.cs`.
2. Ensure provider name matches `VerificationResult.Provider` from your service.
3. Test with `POST /identity` and inspect `ScoreResult.Breakdown` in response to verify calculation.

### New Endpoints
- Keep route prefix `api/v1/[controller]` (currently all routes in `VerificationController`).
- For separate bounded context (e.g., user profile, admin tools), create new controller.
- Update Swagger docs if adding complex DTOs.

### Modifying Decision Logic
- **Thresholds**: Edit switch expressions in `PaymentDecisionEngine` methods (`DetermineAllowedMethods`, `CalculateLimits`).
- **Additional verifications**: Update `GetAdditionalVerificationRequirements` to add/remove steps based on score.
- **Risk levels**: Adjust `WeightedScorer.GetRiskLevel` ranges.

### Test Data Evolution
- Use `TestDataGenerator.GenerateTestSuite()` for standardized scenarios.
- Add new scenarios by extending `TestScenarios` or `PaymentScenarios` static classes.
- Endpoint `/test-scenario/{name}` and `/test-data` support scenario-based generation.

## Security & Testing Notes

- **Security tests** (in `tests/Kashout.Tests/SecurityTests.cs` and `02-security-tests.ts`): Assert no 500s on SQLi/XSS/oversized inputs. MVP does not implement rate limiting; add middleware if needed.
- **Penetration tests** (`PenetrationTests.cs`, `03-penetration-tests.ts`): Simulate RLS bypasses, auth token abuse. Some tests reference Supabase; treat as external integration checks (skip if Supabase not configured).
- **Integration tests** (`VerificationControllerTests.cs`): Use `WebApplicationFactory<Program>` to spin up in-memory API. Validates all endpoints return expected status codes and shapes.
- **Compliance**: `ComplianceChecker` is a stub. Real KYC/AML/OFAC checks would live there; extend `IComplianceChecker` interface as needed.
