#!/usr/bin/env bash
# Test script for Order API endpoints
# Usage: bash .plans/test-orders.sh
# Requirements: The API must be running (dotnet run --project Tibr.API)
#               Users and Products must exist in the database

set -euo pipefail

BASE_URL="http://localhost:5151/api/orders"

# ─── IDs from the seed script (.plans/seed.sql) ───
USER_ID=1
PRODUCT_ID=1

# ─── Pretty-print helpers ───
pass() { echo -e "  \033[32m✓ $1\033[0m"; }
fail() { echo -e "  \033[31m✗ $1\033[0m"; }
sep()  { echo "────────────────────────────────────────────"; }

echo ""
echo "📦 Order API Tests"
sep

# ─── 1. GET all orders ───
echo ""
echo "1. GET /api/orders — List all orders"
RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL")
if [ "$RESPONSE" = "200" ]; then
    pass "Status $RESPONSE"
else
    fail "Expected 200, got $RESPONSE"
fi
echo "   Response:"
curl -s "$BASE_URL" | python3 -m json.tool 2>/dev/null || curl -s "$BASE_URL"
sep

# ─── 2. POST — Create order ───
echo ""
echo "2. POST /api/orders — Create a new order"
CREATE_BODY=$(cat <<EOF
{
  "userId": $USER_ID,
  "items": [
    { "productId": $PRODUCT_ID, "quantity": 2 }
  ]
}
EOF
)
HTTP_CODE=$(curl -s -o /tmp/order_response.json -w "%{http_code}" \
    -X POST "$BASE_URL" \
    -H "Content-Type: application/json" \
    -d "$CREATE_BODY")

if [ "$HTTP_CODE" = "201" ]; then
    pass "Status $HTTP_CODE (Created)"
    echo "   Response:"
    python3 -m json.tool /tmp/order_response.json 2>/dev/null || cat /tmp/order_response.json
else
    fail "Expected 201, got $HTTP_CODE"
    echo "   Response:"
    cat /tmp/order_response.json
    echo ""
    sep
    echo "⚠️  Skipping remaining tests — order creation failed."
    exit 1
fi
sep

# Extract the new order ID
ORDER_ID=$(python3 -c "import json; print(json.load(open('/tmp/order_response.json'))['id'])" 2>/dev/null || echo "")
echo "   → Created order ID: $ORDER_ID"

# ─── 3. GET by ID ───
echo ""
echo "3. GET /api/orders/$ORDER_ID — Get order by ID"
HTTP_CODE=$(curl -s -o /tmp/order_by_id.json -w "%{http_code}" "$BASE_URL/$ORDER_ID")
if [ "$HTTP_CODE" = "200" ]; then
    pass "Status $HTTP_CODE"
    echo "   Response:"
    python3 -m json.tool /tmp/order_by_id.json 2>/dev/null || cat /tmp/order_by_id.json
else
    fail "Expected 200, got $HTTP_CODE"
fi
sep

# ─── 4. GET by User ID ───
echo ""
echo "4. GET /api/orders/user/$USER_ID — Get orders by user"
HTTP_CODE=$(curl -s -o /tmp/orders_by_user.json -w "%{http_code}" "$BASE_URL/user/$USER_ID")
if [ "$HTTP_CODE" = "200" ]; then
    pass "Status $HTTP_CODE"
    echo "   Response:"
    python3 -m json.tool /tmp/orders_by_user.json 2>/dev/null || cat /tmp/orders_by_user.json
else
    fail "Expected 200, got $HTTP_CODE"
fi
sep

# ─── 5. PUT — Update order status ───
echo ""
echo "5. PUT /api/orders/$ORDER_ID — Update order status"
UPDATE_BODY=$(cat <<EOF
{
  "orderStatus": "Shipped",
  "paymentStatus": "Paid"
}
EOF
)
HTTP_CODE=$(curl -s -o /tmp/order_updated.json -w "%{http_code}" \
    -X PUT "$BASE_URL/$ORDER_ID" \
    -H "Content-Type: application/json" \
    -d "$UPDATE_BODY")

if [ "$HTTP_CODE" = "200" ]; then
    pass "Status $HTTP_CODE"
    echo "   Response:"
    python3 -m json.tool /tmp/order_updated.json 2>/dev/null || cat /tmp/order_updated.json
else
    fail "Expected 200, got $HTTP_CODE"
    cat /tmp/order_updated.json
fi
sep

# ─── 6. DELETE — Soft-delete order ───
echo ""
echo "6. DELETE /api/orders/$ORDER_ID — Soft-delete order"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X DELETE "$BASE_URL/$ORDER_ID")
if [ "$HTTP_CODE" = "204" ]; then
    pass "Status $HTTP_CODE (No Content)"
else
    fail "Expected 204, got $HTTP_CODE"
fi
sep

# ─── 7. Confirm deletion (should 404) ───
echo ""
echo "7. GET /api/orders/$ORDER_ID — Confirm deletion (should be 404)"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/$ORDER_ID")
if [ "$HTTP_CODE" = "404" ]; then
    pass "Status $HTTP_CODE (Not Found) ✓ soft delete confirmed"
else
    fail "Expected 404 after deletion, got $HTTP_CODE"
fi
sep

# ─── Summary ───
echo ""
echo "✅ All tests completed"
echo ""
