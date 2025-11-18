#!/bin/bash

echo "🧪 Running Kashout Verification System Tests..."
echo "================================================"
echo ""

# Build the solution
echo "📦 Building solution..."
dotnet build --configuration Release --no-incremental > /dev/null 2>&1

if [ $? -ne 0 ]; then
    echo "❌ Build failed!"
    exit 1
fi

echo "✅ Build successful!"
echo ""

# Run the tests
echo "🔬 Running tests..."
dotnet test tests/Kashout.Tests/Kashout.Tests.csproj --verbosity quiet --nologo

# Store exit code
TEST_RESULT=$?

echo ""
echo "================================================"

if [ $TEST_RESULT -eq 0 ]; then
    echo "✅ All tests passed!"
else
    echo "⚠️  Some tests failed - see details above"
fi

echo ""
echo "📝 For detailed results, see: TEST-RESULTS.md"
echo ""

exit $TEST_RESULT
