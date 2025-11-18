#!/bin/bash

echo "🧪 KASHOUT VERIFICATION SYSTEM - COMPREHENSIVE TEST SUITE"
echo "==========================================================="
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Run functional tests
echo "📝 Phase 1: Functional Tests"
echo "----------------------------"
npx tsx tests/01-functional-tests.ts
FUNC_RESULT=$?
echo ""

# Run security tests
echo "🔐 Phase 2: Security Tests"
echo "--------------------------"
npx tsx tests/02-security-tests.ts
SEC_RESULT=$?
echo ""

# Run penetration tests
echo "🎯 Phase 3: Penetration Tests"
echo "------------------------------"
npx tsx tests/03-penetration-tests.ts
PEN_RESULT=$?
echo ""

# Run the original comprehensive test
echo "✨ Phase 4: Integration Test"
echo "----------------------------"
npx tsx test-verification-system.ts
INT_RESULT=$?
echo ""

# Summary
echo "==========================================================="
echo "📊 FINAL TEST SUMMARY"
echo "==========================================================="

if [ $FUNC_RESULT -eq 0 ]; then
    echo -e "${GREEN}✅ Functional Tests: PASSED${NC}"
else
    echo -e "${RED}❌ Functional Tests: FAILED${NC}"
fi

if [ $SEC_RESULT -eq 0 ]; then
    echo -e "${GREEN}✅ Security Tests: PASSED${NC}"
else
    echo -e "${YELLOW}⚠️  Security Tests: WARNINGS${NC}"
fi

if [ $PEN_RESULT -eq 0 ]; then
    echo -e "${GREEN}✅ Penetration Tests: SECURE${NC}"
else
    echo -e "${RED}❌ Penetration Tests: VULNERABILITIES FOUND${NC}"
fi

if [ $INT_RESULT -eq 0 ]; then
    echo -e "${GREEN}✅ Integration Test: PASSED${NC}"
else
    echo -e "${RED}❌ Integration Test: FAILED${NC}"
fi

echo ""
echo "==========================================================="

# Exit with error if any test failed
if [ $FUNC_RESULT -ne 0 ] || [ $PEN_RESULT -ne 0 ] || [ $INT_RESULT -ne 0 ]; then
    echo -e "${RED}⚠️  SYSTEM NOT READY FOR PRODUCTION${NC}"
    exit 1
else
    echo -e "${GREEN}✅ SYSTEM READY FOR PRODUCTION${NC}"
    exit 0
fi
