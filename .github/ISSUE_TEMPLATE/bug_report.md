name: Bug report
description: Seen a bug? Let us help!
body:
  - type: markdown
    attributes:
      value: |
        # Bedrock Launcher bug report

        Important to note that your issue may have already been reported before. Please check:
        - by searching the error name with [issue tag](https://github.com/BedrockLauncher/BedrockLauncher)
        - If you see you're duplicating someone's other issue, go ahead and provide any informations that may be helpful and do everything that may fix the issue!
  - type: dropdown
    attributes:
      label: Type
      options:
        - Installation
        - Dependencies
        - Other
    validations:
      required: true
  - type: textarea
    attributes:
      label: Bug description
      description: How did you find the bug? Any additional details that might help?
    validations:
      required: true
  - type: textarea
    attributes:
      label: Screenshots or videos
      description: Add screenshots or videos that show the bug here.
      placeholder: Drag and drop the screenshots/videos into this box.
    validations:
      required: false
  - type: input
    attributes:
      label: Version
      description: The version you encountered this bug on.
    validations:
      required: true
  - type: markdown
    attributes:
      value: |
        ## Logs

        Attaching log files is required for every reported bug. See instructions below on how to find them.
    
  - type: textarea
    attributes:
      label: Logs
      placeholder: Drag and drop the log files into this box.
    validations:
      required: true
