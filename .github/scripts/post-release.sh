#!/bin/bash
set -euo pipefail
NOTES_FILE="$1"
if [ ! -f "$NOTES_FILE" ]; then
  echo "No release notes file: $NOTES_FILE"
  exit 0
fi
NOTES=$(cat "$NOTES_FILE")
# Shortened text for tweet-like services
TWEET_TEXT=$(echo "$NOTES" | sed 's/\n/ /g' | cut -c1-270)

# Post to Slack via Incoming Webhook
if [ -n "${SLACK_WEBHOOK_URL:-}" ]; then
  echo "Posting to Slack..."
  payload=$(jq -nc --arg text "${NOTES}" '{text:$text}')
  curl -s -X POST -H 'Content-type: application/json' --data "$payload" "$SLACK_WEBHOOK_URL" || echo "Slack post failed"
else
  echo "SLACK_WEBHOOK_URL not set; skipping Slack"
fi

# Post to X (Twitter) via v2 endpoint - requires OAuth2 user context token with write access
if [ -n "${X_BEARER_TOKEN:-}" ]; then
  echo "Posting to X..."
  curl -s -X POST "https://api.twitter.com/2/tweets" \
    -H "Authorization: Bearer ${X_BEARER_TOKEN}" \
    -H "Content-Type: application/json" \
    -d "{\"text\": \"${TWEET_TEXT//"/\"}\"}" || echo "X post failed"
else
  echo "X_BEARER_TOKEN not set; skipping X"
fi

# Post to Qiita
if [ -n "${QIITA_TOKEN:-}" ]; then
  echo "Posting to Qiita..."
  TITLE="Release - ${GITHUB_RUN_NUMBER:-unknown}"
  BODY="# Release Notes\n\n${NOTES}"
  data=$(jq -nc --arg title "$TITLE" --arg body "$BODY" '{title:$title, body:$body, private:false, tags:[] }')
  curl -s -X POST "https://qiita.com/api/v2/items" \
    -H "Authorization: Bearer ${QIITA_TOKEN}" \
    -H "Content-Type: application/json" \
    -d "$data" || echo "Qiita post failed"
else
  echo "QIITA_TOKEN not set; skipping Qiita"
fi

echo "Done posting (attempted)."
