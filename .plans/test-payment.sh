#!/usr/bin/env bash
# Test script for Payment flow — Order → Initiate → Callback → Verify
#
# Usage:
#   bash .plans/test-payment.sh              # simulated callback
#   bash .plans/test-payment.sh full         # real Paymob sandbox (opens browser)
#
# Requirements:
#   - API running at http://localhost:5151
#   - Users & Products exist in the DB (run .plans/seed.sql first)
#   - python3 with json module available

set -euo pipefail

BASE_URL="http://localhost:5151"
ORDERS_URL="$BASE_URL/api/orders"
PAYMENT_URL="$BASE_URL/api/payment"

MODE="${1:-simulated}"

# ─── IDs from seed script ───
USER_ID=1
PRODUCT_ID=1

# ─── Pretty-print helpers ───
pass() { echo -e "  \033[32m✓ $1\033[0m"; }
fail() { echo -e "  \033[31m✗ $1\033[0m"; }
info() { echo -e "  \033[36m→ $1\033[0m"; }
sep()  { echo "────────────────────────────────────────────"; }

echo ""
echo "💳 Payment Flow Tests — Mode: $MODE"
sep

# ───────────────────────────────────────────────
# Read HMAC secret from appsettings.json
# ───────────────────────────────────────────────
HMAC_SECRET=$(python3 -c "
import json
with open('Tibr.API/appsettings.json') as f:
    print(json.load(f)['Paymob']['HmacSecret'])
")
info "HMAC secret loaded from appsettings.json"

# ───────────────────────────────────────────────
# 1. Create an order
# ───────────────────────────────────────────────
echo ""
echo "1. POST /api/orders — Create order"
CREATE_BODY=$(cat <<EOF
{
  "userId": $USER_ID,
  "items": [
    { "productId": $PRODUCT_ID, "quantity": 2 }
  ]
}
EOF
)

HTTP_CODE=$(curl -s -o /tmp/payment_order.json -w "%{http_code}" \
    -X POST "$ORDERS_URL" \
    -H "Content-Type: application/json" \
    -d "$CREATE_BODY")

if [ "$HTTP_CODE" = "201" ]; then
    pass "Status $HTTP_CODE (Created)"
else
    fail "Expected 201, got $HTTP_CODE"
    cat /tmp/payment_order.json
    echo ""
    exit 1
fi

ORDER_ID=$(python3 -c "import json; print(json.load(open('/tmp/payment_order.json'))['id'])")
info "Order ID: $ORDER_ID"
sep

# ───────────────────────────────────────────────
# 2. Initiate payment
# ───────────────────────────────────────────────
echo ""
echo "2. POST /api/payment/initiate — Initiate payment"
INITIATE_BODY=$(cat <<EOF
{
  "orderId": $ORDER_ID,
  "amountCents": 50000,
  "currency": "EGP",
  "firstName": "Test",
  "lastName": "User",
  "email": "test@example.com",
  "phone": "01000000000"
}
EOF
)

HTTP_CODE=$(curl -s -o /tmp/payment_initiate.json -w "%{http_code}" \
    -X POST "$PAYMENT_URL/initiate" \
    -H "Content-Type: application/json" \
    -d "$INITIATE_BODY")

if [ "$HTTP_CODE" = "200" ]; then
    pass "Status $HTTP_CODE"
    PAYMENT_URL_RESPONSE=$(python3 -c "import json; print(json.load(open('/tmp/payment_initiate.json'))['paymentUrl'])" 2>/dev/null || echo "")
    info "Checkout URL: $PAYMENT_URL_RESPONSE"
else
    fail "Expected 200, got $HTTP_CODE"
    cat /tmp/payment_initiate.json
    echo ""
    exit 1
fi
sep

# ───────────────────────────────────────────────
# 3. Callback simulation or full flow
# ───────────────────────────────────────────────
if [ "$MODE" = "full" ]; then
    echo ""
    echo "3. Full Paymob sandbox flow"
    info "Open this URL in your browser and complete payment:"
    echo ""
    echo "   $PAYMENT_URL_RESPONSE"
    echo ""
    echo -n "   Press Enter after you finish the payment (or type 'skip'): "
    read CONFIRM
    if [ "$CONFIRM" = "skip" ]; then
        info "Skipped verification. Order ID=$ORDER_ID"
        sep
        exit 0
    fi
else
    echo ""
    echo "3. Simulated callback — POST /api/payment/callback/processed"
    echo ""

    CALLBACK_PAYLOAD=$(cat <<EOF
{
  "obj": {
    "id": 99999,
    "success": true,
    "pending": false,
    "amount_cents": 50000,
    "currency": "EGP",
    "created_at": "2026-05-25T12:00:00.000000",
    "error_occured": false,
    "has_parent_transaction": false,
    "integration_id": 0,
    "is_3d_secure": false,
    "is_auth": false,
    "is_capture": false,
    "is_refunded": false,
    "is_standalone_payment": false,
    "is_voided": false,
    "owner": 0,
    "order": {
      "id": 88888,
      "merchant_order_id": "payment:1:$ORDER_ID:$(date +%s)"
    },
    "source_data": {
      "pan": "1234",
      "type": "card",
      "sub_type": "Visa"
    }
  },
  "type": "TRANSACTION"
}
EOF
)

    # ─── Compute HMAC-SHA512 ───
    info "Computing HMAC..."
    echo "$CALLBACK_PAYLOAD" > /tmp/callback_payload.json
    HMAC=$(python3 -c "
import hmac, hashlib, json

with open('/tmp/callback_payload.json') as f:
    payload = json.load(f)
t = payload['obj']

data = ''.join(str(v) for v in [
    t['amount_cents'],
    t['created_at'],
    t['currency'],
    str(t['error_occured']).lower(),
    str(t['has_parent_transaction']).lower(),
    t['id'],
    t['integration_id'],
    str(t['is_3d_secure']).lower(),
    str(t['is_auth']).lower(),
    str(t['is_capture']).lower(),
    str(t['is_refunded']).lower(),
    str(t['is_standalone_payment']).lower(),
    str(t['is_voided']).lower(),
    t['order']['id'] if t.get('order') else '',
    t['owner'],
    str(t['pending']).lower(),
    t.get('source_data', {}).get('pan', ''),
    t.get('source_data', {}).get('sub_type', ''),
    t.get('source_data', {}).get('type', ''),
    str(t['success']).lower(),
])

key = '$HMAC_SECRET'.encode('utf-8')
computed = hmac.new(key, data.encode('utf-8'), hashlib.sha512).hexdigest()
print(computed)
")

    info "HMAC: ${HMAC:0:20}..."

    # ─── Send callback ───
    HTTP_CODE=$(curl -s -o /tmp/callback_response.json -w "%{http_code}" \
        -X POST "$PAYMENT_URL/callback/processed?hmac=$HMAC" \
        -H "Content-Type: application/json" \
        -d "$CALLBACK_PAYLOAD")

    if [ "$HTTP_CODE" = "200" ]; then
        pass "Callback returned $HTTP_CODE"
    else
        fail "Expected 200, got $HTTP_CODE"
        cat /tmp/callback_response.json
        echo ""
    fi
fi

# ───────────────────────────────────────────────
# 4. Verify order payment status
# ───────────────────────────────────────────────
echo ""
echo "4. GET /api/orders/$ORDER_ID — Verify payment status"
echo ""

if [ "$MODE" = "full" ]; then
    info "Waiting for Paymob callback to arrive (polling up to 30s)..."
    PAYMENT_STATUS="Unpaid"
    for i in $(seq 1 15); do
        sleep 2
        HTTP_CODE=$(curl -s -o /tmp/order_after_payment.json -w "%{http_code}" "$ORDERS_URL/$ORDER_ID")
        if [ "$HTTP_CODE" = "200" ]; then
            PAYMENT_STATUS=$(python3 -c "
import json
o = json.load(open('/tmp/order_after_payment.json'))
print(o.get('paymentStatus', 'NOT_FOUND'))
" 2>/dev/null || echo "UNKNOWN")
            if [ "$PAYMENT_STATUS" = "Paid" ]; then
                break
            fi
        fi
    done
else
    HTTP_CODE=$(curl -s -o /tmp/order_after_payment.json -w "%{http_code}" "$ORDERS_URL/$ORDER_ID")
    if [ "$HTTP_CODE" = "200" ]; then
        PAYMENT_STATUS=$(python3 -c "
import json
o = json.load(open('/tmp/order_after_payment.json'))
print(o.get('paymentStatus', 'NOT_FOUND'))
" 2>/dev/null || echo "UNKNOWN")
    else
        PAYMENT_STATUS="FETCH_ERROR"
    fi
fi

echo "   Order ID: $ORDER_ID"
echo "   HTTP Status: $HTTP_CODE"
echo "   Payment Status: $PAYMENT_STATUS"
echo ""

if [ "$PAYMENT_STATUS" = "Paid" ]; then
    pass "paymentStatus = \"Paid\" ✓"
    echo ""
    echo "   Full order response:"
    python3 -m json.tool /tmp/order_after_payment.json 2>/dev/null || cat /tmp/order_after_payment.json
elif [ "$PAYMENT_STATUS" = "FETCH_ERROR" ]; then
    fail "Could not fetch order (HTTP $HTTP_CODE)"
else
    fail "Expected paymentStatus \"Paid\", got \"$PAYMENT_STATUS\""
    echo ""
    echo "   Full order response:"
    python3 -m json.tool /tmp/order_after_payment.json 2>/dev/null || cat /tmp/order_after_payment.json
fi
sep

# ───────────────────────────────────────────────
# Summary
# ───────────────────────────────────────────────
echo ""
echo "✅ Payment test completed"
echo ""
