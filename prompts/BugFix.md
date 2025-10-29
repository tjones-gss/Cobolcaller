# Bug Fix Prompt

## Objective
Identify and fix the reported bug while maintaining code quality and preventing regressions.

## Investigation Steps

### Reproduce the Issue
- Confirm the exact steps to reproduce
- Identify the expected vs. actual behavior
- Determine the scope of impact (how many users/scenarios affected)

### Root Cause Analysis
- Locate the code causing the problem
- Understand why the bug occurs
- Check if this is a symptom of a larger issue
- Review git history - when was this code last changed?

### Impact Assessment
- Are there other areas with similar code patterns?
- Could this bug affect other features?
- What data might be affected?
- Is this a critical/blocking issue?

## Fix Requirements

### Code Changes
- Make the minimal necessary change to fix the issue
- Add defensive programming to prevent recurrence
- Include error handling if missing
- Update comments to explain the fix

### Testing
- Test the specific scenario that failed
- Test edge cases and boundary conditions
- Verify no regressions in related functionality
- Check performance impact

### Documentation
- Document the root cause in commit message
- Update inline comments if logic changed
- Note any workarounds removed
- Add to known issues list if not fully resolved

## Deliverables
- Fixed code with clear explanation
- Description of root cause
- Test cases that now pass
- Any follow-up work needed (tech debt)
- Recommendation to prevent similar bugs

## Quality Checks
- [ ] Bug is fully resolved, not just symptoms
- [ ] Fix doesn't introduce new issues
- [ ] Code follows team standards
- [ ] Adequate error handling included
- [ ] Performance is acceptable
- [ ] Documentation is updated
