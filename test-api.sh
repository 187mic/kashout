#!/bin/bash

echo "🧪 Testing Kashout MVP API..."
echo ""

# Wait for the API to be ready
echo "⏳ Waiting for API to start..."
sleep 2

# Test 1: Get test scenarios
echo "📋 Test 1: Getting test scenarios..."
curl -s http://localhost:5000/api/v1/verification/test-scenarios | jq '.' || echo "Test failed"
echo ""
echo ""

# Test 2: Generate low-risk test data
echo "🧑 Test 2: Generating low-risk test data..."
curl -s -X POST http://localhost:5000/api/v1/verification/test-data \
  -H "Content-Type: application/json" \
  -d '{"scenario":"low_risk"}' | jq '.' || echo "Test failed"
echo ""
echo ""

# Test 3: Generate high-risk test data
echo "⚠️  Test 3: Generating high-risk test data..."
curl -s -X POST http://localhost:5000/api/v1/verification/test-data \
  -H "Content-Type: application/json" \
  -d '{"scenario":"high_risk"}' | jq '.' || echo "Test failed"
echo ""
echo ""

# Test 4: Generate fraudulent test data
echo "🚨 Test 4: Generating fraudulent test data..."
curl -s -X POST http://localhost:5000/api/v1/verification/test-data \
  -H "Content-Type: application/json" \
  -d '{"scenario":"fraudulent"}' | jq '.' || echo "Test failed"
echo ""

echo "✅ API tests complete!"
