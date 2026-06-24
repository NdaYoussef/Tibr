#!/usr/bin/env bash
# Test script for Withdraw + Review endpoints
#
# Usage:
#   bash .plans/test-withdraw-review.sh
#
# Requirements:
#   - API running at http://localhost:5151
#   - A user exists with BCrypt-hashed password "Test@123"
#     (run .plans/seed.sql, then update password hash manually, or
#      register via POST /api/auth/register and verify OTP)
#   - An order exists for that user (run .plans/seed.sql or order test)
#   - python3 with json module available
#
# Prerequisites for the seed user (User ID 1):
#   The seed SQL sets password to placeholder 'hashed_pwd_1'.
#   To make login work, UPDATE the password to a real BCrypt hash:
#     UPDATE Users SET Password = '$2a$11$...' WHERE Id = 1;
#   Generate the hash with: dotnet run --project tools/HashPassword -- "Test@123"

set -euo pipefail

BASE_URL="http://localhost:5151"
AUTH_URL="$BASE_URL/api/auth"
WITHDRAW_URL="$BASE_URL/api/withdraw"
REVIEWS_URL="$BASE_URL/api/reviews"

API_RUNNING=false
TOKEN=""
USER_ID=""

# ─── Helpers ───
pass() { echo -e "  \033[32m✓ $1\033[0m"; }
fail() { echo -e "  \033[31m✗ $1\033[0m"; }
info() { echo -e "  \033[36m→ $1\033[0m"; }
sep()  { echo "────────────────────────────────────────────"; }
check_api() {
  if ! curl -sf "$BASE_URL/api/auth/login" -X POST -H "Content-Type: application/json" \
       -d '{"email":"","password":""}' > /dev/null 2>&1; then
    echo -e "\n  \033[31m⚠ API is not running at $BASE_URL\033[0m"
    echo "  Start it with: dotnet run --project Tibr.API"
    exit 1
  fi
}

echo ""
echo "🏧 Withdraw + Review API Tests"
sep

# ─── 0. Login ───
echo ""
echo "0. POST /api/auth/login — Get JWT token"
info "Using email: eslamlegend5@gmail.com / password: Test@123"
LOGIN_RESPONSE=$(curl -s -X POST "$AUTH_URL/login" \
  -H "Content-Type: application/json" \
  -d '{"email": "eslamlegend5@gmail.com", "password": "Test@123", "rememberMe": false}')

TOKEN=$(echo "$LOGIN_RESPONSE" | python3 -c "
import json, sys
try:
    d = json.load(sys.stdin)
    print(d.get('token', ''))
except: print('')
")

if [ -z "$TOKEN" ]; then
  fail "Login failed — no token returned"
  echo "   Response: $LOGIN_RESPONSE"
  echo ""
  sep
  echo "⚠  See prerequisites at the top of this script."
  exit 1
fi

USER_ID=$(echo "$LOGIN_RESPONSE" | python3 -c "
import json, sys
try:
    d = json.load(sys.stdin)
    print(d.get('userId', ''))
except: print('')
")
info "Token obtained (${#TOKEN} chars)"
info "User ID: $USER_ID"
pass "Login successful"
sep

AUTH_HEADER="Authorization: Bearer $TOKEN"

# ═══════════════════════════════════════════════════════
# WITHDRAW FLOW
# ═══════════════════════════════════════════════════════

echo ""
echo "═══ Withdraw Flow ═══"
sep

# ─── 1. POST /api/withdraw — Valid withdrawal ───
echo ""
echo "1. POST /api/withdraw — Create valid withdrawal (Amount: 5000)"
HTTP_CODE=$(curl -s -o /tmp/withdraw_create.json -w "%{http_code}" \
  -X POST "$WITHDRAW_URL" \
  -H "Content-Type: application/json" \
  -H "$AUTH_HEADER" \
  -d '{"amount": 5000, "type": "Bank", "name": "My Bank", "number": "EG1234567890123456"}')

if [ "$HTTP_CODE" = "201" ]; then
  pass "Status $HTTP_CODE (Created)"
else
  fail "Expected 201, got $HTTP_CODE"
  cat /tmp/withdraw_create.json
fi
sep

# ─── 2. POST /api/withdraw — Invalid amount (below min) ───
echo ""
echo "2. POST /api/withdraw — Invalid amount (50 — below 100 min)"
HTTP_CODE=$(curl -s -o /tmp/withdraw_invalid_amount.json -w "%{http_code}" \
  -X POST "$WITHDRAW_URL" \
  -H "Content-Type: application/json" \
  -H "$AUTH_HEADER" \
  -d '{"amount": 50, "type": "Bank", "name": "My Bank", "number": "EG1234567890123456"}')

if [ "$HTTP_CODE" = "400" ]; then
  pass "Status $HTTP_CODE (Bad Request) — correctly rejected"
  echo "   Message: $(cat /tmp/withdraw_invalid_amount.json)"
else
  fail "Expected 400, got $HTTP_CODE"
fi
sep

# ─── 3. POST /api/withdraw — Invalid amount (above max) ───
echo ""
echo "3. POST /api/withdraw — Invalid amount (60000 — above 50000 max)"
HTTP_CODE=$(curl -s -o /tmp/withdraw_over_limit.json -w "%{http_code}" \
  -X POST "$WITHDRAW_URL" \
  -H "Content-Type: application/json" \
  -H "$AUTH_HEADER" \
  -d '{"amount": 60000, "type": "Bank", "name": "My Bank", "number": "EG1234567890123456"}')

if [ "$HTTP_CODE" = "400" ]; then
  pass "Status $HTTP_CODE (Bad Request) — correctly rejected"
  echo "   Message: $(cat /tmp/withdraw_over_limit.json)"
else
  fail "Expected 400, got $HTTP_CODE"
fi
sep

# ─── 4. POST /api/withdraw — Missing name ───
echo ""
echo "4. POST /api/withdraw — Missing name"
HTTP_CODE=$(curl -s -o /tmp/withdraw_no_name.json -w "%{http_code}" \
  -X POST "$WITHDRAW_URL" \
  -H "Content-Type: application/json" \
  -H "$AUTH_HEADER" \
  -d '{"amount": 1000, "type": "EWallet", "name": "", "number": "01000000000"}')

if [ "$HTTP_CODE" = "400" ]; then
  pass "Status $HTTP_CODE (Bad Request) — correctly rejected"
  echo "   Message: $(cat /tmp/withdraw_no_name.json)"
else
  fail "Expected 400, got $HTTP_CODE"
fi
sep

# ─── 5. POST /api/withdraw — Unauthenticated ───
echo ""
echo "5. POST /api/withdraw — No auth token (should be 401)"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" \
  -X POST "$WITHDRAW_URL" \
  -H "Content-Type: application/json" \
  -d '{"amount": 1000, "type": "Bank", "name": "Test", "number": "123"}')

if [ "$HTTP_CODE" = "401" ]; then
  pass "Status $HTTP_CODE (Unauthorized)"
else
  fail "Expected 401, got $HTTP_CODE"
fi
sep

# ═══════════════════════════════════════════════════════
# REVIEW FLOW
# ═══════════════════════════════════════════════════════

echo ""
echo "═══ Review Flow ═══"
sep

ORDER_ID=1

# ─── 6. POST /api/reviews — Create valid review ───
echo ""
echo "6. POST /api/reviews — Create valid review (Value: 5)"
HTTP_CODE=$(curl -s -o /tmp/review_create.json -w "%{http_code}" \
  -X POST "$REVIEWS_URL" \
  -H "Content-Type: application/json" \
  -H "$AUTH_HEADER" \
  -d "{\"orderId\": $ORDER_ID, \"description\": \"Great product!\", \"value\": 5}")

if [ "$HTTP_CODE" = "201" ]; then
  pass "Status $HTTP_CODE (Created)"
else
  fail "Expected 201, got $HTTP_CODE"
  cat /tmp/review_create.json
fi
sep

# ─── 7. POST /api/reviews — Duplicate (same OrderId + UserId) ───
echo ""
echo "7. POST /api/reviews — Duplicate review (same OrderId + UserId)"
HTTP_CODE=$(curl -s -o /tmp/review_duplicate.json -w "%{http_code}" \
  -X POST "$REVIEWS_URL" \
  -H "Content-Type: application/json" \
  -H "$AUTH_HEADER" \
  -d "{\"orderId\": $ORDER_ID, \"description\": \"Trying again\", \"value\": 4}")

if [ "$HTTP_CODE" = "400" ]; then
  pass "Status $HTTP_CODE (Bad Request) — duplicate correctly rejected"
  echo "   Message: $(cat /tmp/review_duplicate.json)"
else
  fail "Expected 400, got $HTTP_CODE"
  cat /tmp/review_duplicate.json
fi
sep

# ─── 8. POST /api/reviews — Invalid value (0) ───
echo ""
echo "8. POST /api/reviews — Invalid value (0 — below 1 min)"
HTTP_CODE=$(curl -s -o /tmp/review_low_value.json -w "%{http_code}" \
  -X POST "$REVIEWS_URL" \
  -H "Content-Type: application/json" \
  -H "$AUTH_HEADER" \
  -d "{\"orderId\": 999, \"description\": \"Bad\", \"value\": 0}")

if [ "$HTTP_CODE" = "400" ]; then
  pass "Status $HTTP_CODE (Bad Request) — correctly rejected"
  echo "   Message: $(cat /tmp/review_low_value.json)"
else
  fail "Expected 400, got $HTTP_CODE"
fi
sep

# ─── 9. POST /api/reviews — Invalid value (6) ───
echo ""
echo "9. POST /api/reviews — Invalid value (6 — above 5 max)"
HTTP_CODE=$(curl -s -o /tmp/review_high_value.json -w "%{http_code}" \
  -X POST "$REVIEWS_URL" \
  -H "Content-Type: application/json" \
  -H "$AUTH_HEADER" \
  -d "{\"orderId\": 999, \"description\": \"Bad\", \"value\": 6}")

if [ "$HTTP_CODE" = "400" ]; then
  pass "Status $HTTP_CODE (Bad Request) — correctly rejected"
  echo "   Message: $(cat /tmp/review_high_value.json)"
else
  fail "Expected 400, got $HTTP_CODE"
fi
sep

# Extract the review ID we just created (from step 6)
REVIEW_ID=$(python3 -c "
import json
try:
    # We can't get the ID from create (returns 201 no content).
    # Fall back to getting the first review from the list endpoint.
    print('')
except: print('')
" 2>/dev/null || echo "")

# ─── 10. GET /api/reviews — List my reviews ───
echo ""
echo "10. GET /api/reviews — Get my reviews"
HTTP_CODE=$(curl -s -o /tmp/reviews_list.json -w "%{http_code}" \
  -X GET "$REVIEWS_URL" \
  -H "$AUTH_HEADER")

if [ "$HTTP_CODE" = "200" ]; then
  pass "Status $HTTP_CODE (OK)"
  echo "   Response:"
  python3 -m json.tool /tmp/reviews_list.json 2>/dev/null || cat /tmp/reviews_list.json
else
  fail "Expected 200, got $HTTP_CODE"
fi
sep

# Extract first review ID from the list for the update test
REVIEW_ID=$(python3 -c "
import json
try:
    data = json.load(open('/tmp/reviews_list.json'))
    if isinstance(data, list) and len(data) > 0:
        print(data[0]['id'])
    else:
        print('')
except: print('')
" 2>/dev/null || echo "")

if [ -n "$REVIEW_ID" ]; then
  info "Using Review ID: $REVIEW_ID for update test"

  # ─── 11. PUT /api/reviews/{id} — Update review ───
  echo ""
  echo "11. PUT /api/reviews/$REVIEW_ID — Update description and value"
  HTTP_CODE=$(curl -s -o /tmp/review_update.json -w "%{http_code}" \
    -X PUT "$REVIEWS_URL/$REVIEW_ID" \
    -H "Content-Type: application/json" \
    -H "$AUTH_HEADER" \
    -d '{"description": "Updated review text", "value": 4}')

  if [ "$HTTP_CODE" = "200" ]; then
    pass "Status $HTTP_CODE (OK)"
  else
    fail "Expected 200, got $HTTP_CODE"
    cat /tmp/review_update.json
  fi
  sep

  # ─── 12. PUT /api/reviews/{id} — Both null ───
  echo ""
  echo "12. PUT /api/reviews/$REVIEW_ID — Both fields null (should be 400)"
  HTTP_CODE=$(curl -s -o /tmp/review_update_null.json -w "%{http_code}" \
    -X PUT "$REVIEWS_URL/$REVIEW_ID" \
    -H "Content-Type: application/json" \
    -H "$AUTH_HEADER" \
    -d '{}')

  if [ "$HTTP_CODE" = "400" ]; then
    pass "Status $HTTP_CODE (Bad Request) — correctly rejected"
    echo "   Message: $(cat /tmp/review_update_null.json)"
  else
    fail "Expected 400, got $HTTP_CODE"
  fi
  sep

  # ─── 13. PUT /api/reviews/{id} — Only description ───
  echo ""
  echo "13. PUT /api/reviews/$REVIEW_ID — Only update description"
  HTTP_CODE=$(curl -s -o /tmp/review_update_desc.json -w "%{http_code}" \
    -X PUT "$REVIEWS_URL/$REVIEW_ID" \
    -H "Content-Type: application/json" \
    -H "$AUTH_HEADER" \
    -d '{"description": "Only description changed"}')

  if [ "$HTTP_CODE" = "200" ]; then
    pass "Status $HTTP_CODE (OK)"
  else
    fail "Expected 200, got $HTTP_CODE"
    cat /tmp/review_update_desc.json
  fi
  sep

else
  info "No review found to update — skipping steps 11-13"
  sep
fi

# ─── 14. POST /api/reviews — Non-existent order ───
echo ""
echo "14. POST /api/reviews — Non-existent order (OrderId: 99999)"
HTTP_CODE=$(curl -s -o /tmp/review_bad_order.json -w "%{http_code}" \
  -X POST "$REVIEWS_URL" \
  -H "Content-Type: application/json" \
  -H "$AUTH_HEADER" \
  -d '{"orderId": 99999, "description": "Test", "value": 3}')

if [ "$HTTP_CODE" = "400" ]; then
  pass "Status $HTTP_CODE (Bad Request) — correctly rejected"
  echo "   Message: $(cat /tmp/review_bad_order.json)"
else
  fail "Expected 400, got $HTTP_CODE"
fi
sep

# ─── 15. GET /api/reviews — Unauthenticated ───
echo ""
echo "15. GET /api/reviews — No auth token (should be 401)"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X GET "$REVIEWS_URL")

if [ "$HTTP_CODE" = "401" ]; then
  pass "Status $HTTP_CODE (Unauthorized)"
else
  fail "Expected 401, got $HTTP_CODE"
fi
sep

# ═══════════════════════════════════════════════════════
# SUMMARY
# ═══════════════════════════════════════════════════════
echo ""
echo "✅ All tests completed"
echo ""
echo "Summary of tested scenarios:"
echo "  Withdraw:"
echo "    • Valid creation          → 201"
echo "    • Amount below min (100)  → 400"
echo "    • Amount above max (50k)  → 400"
echo "    • Missing name            → 400"
echo "    • No auth token           → 401"
echo ""
echo "  Reviews:"
echo "    • Valid creation          → 201"
echo "    • Duplicate (Order+User)  → 400"
echo "    • Value below 1           → 400"
echo "    • Value above 5           → 400"
echo "    • List my reviews         → 200"
echo "    • Update review           → 200"
echo "    • Both fields null        → 400"
echo "    • Partial update (desc)   → 200"
echo "    • Non-existent order      → 400"
echo "    • No auth token           → 401"
echo ""
