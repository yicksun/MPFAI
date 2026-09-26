param(
    [string]$Api = "http://localhost:5000",
    [Guid]$OrganizationId = "8182a48a-ad19-41a8-b7e6-dc13d7bb20c7"
)

$ErrorActionPreference = "Stop"
$query = "organizationId=$OrganizationId"
$customer = Invoke-RestMethod -Method Post -Uri "$Api/api/v1/customers?$query" -ContentType "application/json" -Body (@{
    name = "Contoso Sample Customer"
    domain = "contoso.example"
} | ConvertTo-Json)
$engagement = Invoke-RestMethod -Method Post -Uri "$Api/api/v1/engagements?$query" -ContentType "application/json" -Body (@{
    customerId = $customer.id
    name = "Azure modernization pilot"
} | ConvertTo-Json)

$text = [IO.File]::ReadAllText((Join-Path $PSScriptRoot "..\sample-data\funding-guideline.txt"))
$guideline = Invoke-RestMethod -Method Post -Uri "$Api/api/v1/guidelines?$query" -ContentType "application/json" -Body (@{
    fileName = "funding-guideline.txt"
    base64Content = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($text))
    programId = "illustrative-local-pilot"
    version = "1.0"
    rate = 0.15
    cap = 5000
    currency = "USD"
    effectiveFrom = "2026-01-01"
    effectiveTo = "2026-12-31"
    uploader = "sample-uploader"
} | ConvertTo-Json)

$approved = Invoke-RestMethod -Method Post -Uri "$Api/api/v1/guidelines/$($guideline.id)/approve?$query" -ContentType "application/json" -Body (@{
    approver = "sample-approver"
} | ConvertTo-Json)
$task = Invoke-RestMethod -Method Post -Uri "$Api/api/v1/engagements/$($engagement.id)/tasks?$query" -ContentType "application/json" -Body (@{
    title = "Review SOW and confirm program assumptions"
    owner = "sample-delivery-manager"
    dueDate = "2026-10-15"
} | ConvertTo-Json)

[pscustomobject]@{
    OrganizationId = $OrganizationId
    CustomerId = $customer.id
    EngagementId = $engagement.id
    ApprovedGuidelineId = $approved.id
    TaskId = $task.id
}
