# certificate-checker
Checks certificate age on specified websites and email a report.

# Settings file (setting.json)
```json
{
  "SendGrid": "<API Key for sendgrid>",
  "Sites": [
    "https://github.com",
    "https://google.com"
  ],
  "From": {
    "Email": "whatever@example.com",
    "Display": "Whatever"
  },
  "To": [
    {
      "Email": "someone@example.com",
      "Display": "Someone"
    }
  ]
}
```
