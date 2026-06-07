namespace Data;

public record LoggerData(
    DateTime time,
    string eventType,
    int ballId,
    double x,
    double y,
    double velX,
    double velY
    );