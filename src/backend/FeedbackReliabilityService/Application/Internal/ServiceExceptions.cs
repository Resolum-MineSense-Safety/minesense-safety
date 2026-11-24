namespace FeedbackReliabilityService.Application.Internal;

public class FeedbackNotFoundException() : Exception("The alert is not available for feedback.");
public class FeedbackForbiddenException() : Exception("The actor cannot access this operation or alert.");
public class AlertSourceUnavailableException() : Exception("AlertService is unavailable; retry later.");
