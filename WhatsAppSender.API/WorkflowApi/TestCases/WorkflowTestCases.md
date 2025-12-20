# Campaign Workflow API Test Cases (CORRECTED)

**Last Updated**: 2025-11-25
**Status**: Validated against `CampaignsController.cs`

## Test Environment Setup
- **Base URL**: `https://localhost:5001/api`
- **Required Header**: `X-API-Key: {valid_api_key}`

---

## 1. START CAMPAIGN API Tests

### TC-START-001: Start Campaign Successfully (Auto Timing)
- **Endpoint**: `POST /api/campaigns/{id}/start`
- **Preconditions**: Campaign exists with status `Pending`, user has pending selected contacts
- **Request Body**:
```json
{
  "timingMode": "auto",
  "browserType": "chrome"
}
```
- **Expected Response**: `200 OK`
- **Expected Body** (CORRECTED):
```json
{
  "message": "Campaign started successfully",
  "status": "Running",
  "timing_mode": "Auto",
  "timing_settings": {
    "mode": "auto",
    "min_delay": null,
    "max_delay": null,
    "message": "Using database timing settings"
  },
  "pending_contacts": <number>
}
```

### TC-START-002: Start Campaign with Manual Timing
- **Endpoint**: `POST /api/campaigns/{id}/start`
- **Preconditions**: Campaign exists with status `Pending`
- **Request Body**:
```json
{
  "timingMode": "manual",
  "manualTiming": {
    "minDelay": 30,
    "maxDelay": 60
  },
  "browserType": "chrome"
}
```
- **Expected Response**: `200 OK`
- **Expected Body**:
```json
{
  "message": "Campaign started successfully",
  "status": "Running",
  "timing_mode": "Manual",
  "timing_settings": {
    "mode": "manual",
    "min_delay": 30,
    "max_delay": 60
  },
  "pending_contacts": <number>
}
```

### TC-START-003: Start Campaign - Invalid API Key
- **Endpoint**: `POST /api/campaigns/{id}/start`
- **Headers**: `X-API-Key: invalid_key`
- **Expected Response**: `401 Unauthorized`
- **Expected Body**:
```json
{
  "error": "Invalid API key"
}
```

### TC-START-004: Start Campaign - Missing API Key
- **Endpoint**: `POST /api/campaigns/{id}/start`
- **Headers**: No X-API-Key header
- **Expected Response**: `401 Unauthorized`
- **Expected Body**:
```json
{
  "error": "API key is required"
}
```

### TC-START-005: Start Campaign - Already Running
- **Endpoint**: `POST /api/campaigns/{id}/start`
- **Preconditions**: Campaign status is `Running`
- **Expected Response**: `400 Bad Request`
- **Expected Body**:
```json
{
  "error": "Campaign is already running"
}
```

### TC-START-006: Start Campaign - Stopped Campaign
- **Endpoint**: `POST /api/campaigns/{id}/start`
- **Preconditions**: Campaign status is `Stopped`
- **Expected Response**: `400 Bad Request`
- **Expected Body**:
```json
{
  "error": "Stopped campaigns cannot be restarted. Create a new campaign instead."
}
```

### TC-START-007: Start Campaign - Completed Campaign
- **Endpoint**: `POST /api/campaigns/{id}/start`
- **Preconditions**: Campaign status is `Completed`
- **Expected Response**: `400 Bad Request`
- **Expected Body**:
```json
{
  "error": "Campaign has already completed"
}
```

### TC-START-008: Start Campaign - No Pending Contacts
- **Endpoint**: `POST /api/campaigns/{id}/start`
- **Preconditions**: No pending selected contacts for user (`IsSelected = true`)
- **Expected Response**: `400 Bad Request`
- **Expected Body**:
```json
{
  "error": "No pending contacts found for this user"
}
```

### TC-START-009: Start Campaign - Campaign Not Found
- **Endpoint**: `POST /api/campaigns/99999/start`
- **Expected Response**: `404 Not Found`
- **Expected Body**:
```json
{
  "error": "Campaign not found"
}
```

### TC-START-010: Start Campaign - Invalid Template Variables
- **Endpoint**: `POST /api/campaigns/{id}/start`
- **Preconditions**: Campaign message contains invalid template variables not in contact data
- **Expected Response**: `400 Bad Request`
- **Expected Body**:
```json
{
  "error": "Template validation failed",
  "validation_errors": ["..."],
  "variables_found": ["..."],
  "message": "..."
}
```

### TC-START-011: Start Campaign - Firefox Browser
- **Endpoint**: `POST /api/campaigns/{id}/start`
- **Request Body**:
```json
{
  "timingMode": "auto",
  "browserType": "firefox"
}
```
- **Expected Response**: `200 OK`

### TC-START-012: Start Campaign - Empty Body (NEW)
- **Endpoint**: `POST /api/campaigns/{id}/start`
- **Request Body**: `{}` or no body
- **Expected Response**: `200 OK` (uses defaults: auto timing, chrome browser)

### TC-START-013: Resume Paused Campaign (NEW)
- **Endpoint**: `POST /api/campaigns/{id}/start`
- **Preconditions**: Campaign status is `Paused`
- **Expected Response**: `200 OK`
- **Note**: Paused campaigns CAN be resumed by calling start again

---

## 2. PAUSE CAMPAIGN API Tests

### TC-PAUSE-001: Pause Running Campaign Successfully
- **Endpoint**: `POST /api/campaigns/{id}/pause`
- **Preconditions**: Campaign status is `Running`
- **Request Body**:
```json
{
  "currentProgress": 50
}
```
- **Expected Response**: `200 OK`
- **Expected Body**:
```json
{
  "message": "Campaign paused successfully",
  "status": "Paused",
  "currentProgress": 50
}
```

### TC-PAUSE-002: Pause Campaign - Invalid API Key
- **Endpoint**: `POST /api/campaigns/{id}/pause`
- **Headers**: `X-API-Key: invalid_key`
- **Expected Response**: `401 Unauthorized`
- **Expected Body**:
```json
{
  "error": "Invalid API key"
}
```

### TC-PAUSE-003: Pause Campaign - Not Running
- **Endpoint**: `POST /api/campaigns/{id}/pause`
- **Preconditions**: Campaign status is `Pending` or `Paused` or `Stopped`
- **Expected Response**: `400 Bad Request`
- **Expected Body**:
```json
{
  "error": "Only running campaigns can be paused"
}
```

### TC-PAUSE-004: Pause Campaign - Campaign Not Found
- **Endpoint**: `POST /api/campaigns/99999/pause`
- **Expected Response**: `404 Not Found`
- **Expected Body**:
```json
{
  "error": "Campaign not found"
}
```

### TC-PAUSE-005: Pause Campaign - Without Progress Body
- **Endpoint**: `POST /api/campaigns/{id}/pause`
- **Preconditions**: Campaign status is `Running`
- **Request Body**: `{}` or `null`
- **Expected Response**: `200 OK`
- **Note**: Progress will NOT be updated (request is null)

### TC-PAUSE-006: Pause Campaign - Progress Zero (NEW)
- **Endpoint**: `POST /api/campaigns/{id}/pause`
- **Request Body**:
```json
{
  "currentProgress": 0
}
```
- **Expected Response**: `200 OK`
- **Note**: Progress WILL be saved as 0 (due to `>= 0` check in code)

---

## 3. STOP CAMPAIGN API Tests

### TC-STOP-001: Stop Running Campaign Successfully
- **Endpoint**: `POST /api/campaigns/{id}/stop`
- **Preconditions**: Campaign status is `Running`
- **Request Body**:
```json
{
  "currentProgress": 100
}
```
- **Expected Response**: `200 OK`
- **Expected Body**:
```json
{
  "message": "Campaign stopped successfully. Browser session closed and all pending messages cancelled.",
  "status": "Stopped"
}
```

### TC-STOP-002: Stop Campaign - Invalid API Key
- **Endpoint**: `POST /api/campaigns/{id}/stop`
- **Headers**: `X-API-Key: invalid_key`
- **Expected Response**: `401 Unauthorized`
- **Expected Body**:
```json
{
  "error": "Invalid API key"
}
```

### TC-STOP-003: Stop Paused Campaign (CORRECTED)
- **Endpoint**: `POST /api/campaigns/{id}/stop`
- **Preconditions**: Campaign status is `Paused`
- **Expected Response**: `200 OK`
- **Note**: Paused campaigns CAN be stopped

### TC-STOP-004: Stop Pending Campaign (CORRECTED)
- **Endpoint**: `POST /api/campaigns/{id}/stop`
- **Preconditions**: Campaign status is `Pending`
- **Expected Response**: `200 OK`
- **Note**: Pending campaigns CAN be stopped (no status restriction in code)

### TC-STOP-005: Stop Campaign - Campaign Not Found
- **Endpoint**: `POST /api/campaigns/99999/stop`
- **Expected Response**: `404 Not Found`
- **Expected Body**:
```json
{
  "error": "Campaign not found"
}
```

### TC-STOP-006: Stop Campaign - Browser Closes
- **Endpoint**: `POST /api/campaigns/{id}/stop`
- **Preconditions**: Campaign is running with Chrome open
- **Expected Behavior**: All Chrome/ChromeDriver processes killed via PowerShell, taskkill, and .NET Process.Kill()
- **Expected Response**: `200 OK`

### TC-STOP-007: Stop Already Stopped Campaign (NEW)
- **Endpoint**: `POST /api/campaigns/{id}/stop`
- **Preconditions**: Campaign status is already `Stopped`
- **Expected Response**: `200 OK`
- **Note**: Code allows stopping already-stopped campaigns

### TC-STOP-008: Stop Completed Campaign (NEW)
- **Endpoint**: `POST /api/campaigns/{id}/stop`
- **Preconditions**: Campaign status is `Completed`
- **Expected Response**: `200 OK`
- **Note**: Code allows stopping completed campaigns

---

## 4. FORCE CLOSE BROWSERS API Tests

### TC-FORCE-001: Force Close All Browsers
- **Endpoint**: `POST /api/campaigns/force-close-browsers`
- **Expected Response**: `200 OK`
- **Expected Body**:
```json
{
  "message": "Successfully force closed all Chrome browsers. Killed X processes.",
  "processesKilled": <number>,
  "timestamp": "<datetime>"
}
```

### TC-FORCE-002: Force Close - No Browsers Running
- **Endpoint**: `POST /api/campaigns/force-close-browsers`
- **Preconditions**: No Chrome processes running
- **Expected Response**: `200 OK`
- **Expected Body**:
```json
{
  "message": "Successfully force closed all Chrome browsers. Killed 0 processes.",
  "processesKilled": 0,
  "timestamp": "<datetime>"
}
```

### TC-FORCE-003: Force Close - No API Key (FIXED)
- **Endpoint**: `POST /api/campaigns/force-close-browsers`
- **Headers**: No X-API-Key header
- **Expected Response**: `401 Unauthorized`
- **Expected Body**:
```json
{
  "error": "API key is required"
}
```
- **Status**: FIXED - API key validation now enforced

---

## 5. CAMPAIGN PROGRESS API Tests

### TC-PROGRESS-001: Get Campaign Progress Successfully
- **Endpoint**: `GET /api/campaigns/{id}/progress`
- **Preconditions**: Campaign exists
- **Expected Response**: `200 OK`
- **Expected Body**:
```json
{
  "campaign_id": <id>,
  "campaign_name": "<name>",
  "status": "<status>",
  "progress": {
    "total_contacts": <number>,
    "processed": <number>,
    "pending": <number>,
    "percentage": <decimal>
  },
  "statistics": {
    "sent": <number>,
    "delivered": <number>,
    "failed": <number>,
    "success_rate": <decimal>
  },
  "timing": {
    "started_at": "<datetime>|null",
    "last_updated": "<datetime>|null",
    "estimated_completion": "<datetime>|null",
    "estimated_time_remaining_seconds": <number>|null
  },
  "errors": {
    "count": <number>,
    "last_error": "<string>|null"
  }
}
```

### TC-PROGRESS-002: Get Progress - Invalid API Key
- **Endpoint**: `GET /api/campaigns/{id}/progress`
- **Headers**: `X-API-Key: invalid_key`
- **Expected Response**: `401 Unauthorized`

### TC-PROGRESS-003: Get Progress - Campaign Not Found
- **Endpoint**: `GET /api/campaigns/99999/progress`
- **Expected Response**: `404 Not Found`
- **Expected Body**:
```json
{
  "error": "Campaign not found"
}
```

---

## 6. CAMPAIGN CRUD Tests

### TC-CREATE-001: Create Campaign Successfully
- **Endpoint**: `POST /api/campaigns`
- **Request Body**:
```json
{
  "name": "Test Campaign",
  "description": "Test Description",
  "campaignTemplateId": 1,
  "messageContent": "Hello {Name}!",
  "totalContacts": 10,
  "useGenderTemplates": false
}
```
- **Expected Response**: `201 Created` (NOT 200!)
- **Note**: Returns `CreatedAtAction` which is 201

### TC-DELETE-001: Delete Campaign Successfully
- **Endpoint**: `DELETE /api/campaigns/{id}`
- **Preconditions**: Campaign exists and is NOT running
- **Expected Response**: `200 OK`
- **Expected Body**:
```json
{
  "message": "Campaign deleted successfully"
}
```

### TC-DELETE-002: Delete Running Campaign (NEW)
- **Endpoint**: `DELETE /api/campaigns/{id}`
- **Preconditions**: Campaign status is `Running`
- **Expected Response**: `400 Bad Request`
- **Expected Body**:
```json
{
  "error": "Cannot delete a running campaign. Stop it first."
}
```

---

## 7. WORKFLOW INTEGRATION Tests

### TC-WORKFLOW-001: Full Workflow - Start, Pause, Resume, Stop
1. Create campaign - verify `201 Created`
2. Start campaign - verify status `Running`
3. Get progress - verify data returned
4. Pause campaign - verify status `Paused`
5. Resume campaign (start again) - verify status `Running`
6. Stop campaign - verify status `Stopped`
7. Verify browser closed
8. Try to start again - should fail with "Stopped campaigns cannot be restarted"

### TC-WORKFLOW-002: Cannot Restart Stopped Campaign
1. Create campaign
2. Start campaign
3. Stop campaign
4. Try to start again
5. **Expected**: `400 Bad Request` with error "Stopped campaigns cannot be restarted. Create a new campaign instead."

### TC-WORKFLOW-003: Pause and Resume with Progress
1. Create campaign with 100 contacts
2. Start campaign
3. Wait until some messages sent
4. Pause campaign with `currentProgress: 50`
5. Verify progress saved in response
6. Resume campaign (call start)
7. Verify continues and completes

---

## Test Data Requirements

### Valid Campaign Data
```json
{
  "name": "Test Campaign",
  "description": "Test Description",
  "campaignTemplateId": 1,
  "messageContent": "Hello {Name}!",
  "totalContacts": 10,
  "useGenderTemplates": false
}
```

### Valid Contacts Data
- At least 10 contacts with:
  - `Status = Pending`
  - `IsSelected = true`
  - `UserId` matching API key owner

---

## Known Issues (From Code Review)

| Issue | Severity | Status |
|-------|----------|--------|
| Force-close-browsers has no API key validation | CRITICAL | FIXED |
| Pause progress check uses `>= 0` (always saves 0) | MEDIUM | FIXED |
| Pause partial DB update on failure | MEDIUM | FIXED |
| Stop allows stopping any status campaign | LOW | BY DESIGN |

## Fixes Applied (2025-11-25)

1. Added API key validation to `force-close-browsers` endpoint
2. Changed pause progress check from `>= 0` to `> 0` with explicit null check
3. Wrapped pause operation in transaction for atomicity
4. Added comprehensive Swagger documentation to all endpoints
