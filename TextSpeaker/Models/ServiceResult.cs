namespace TextSpeaker.Models;

public record ServiceResult<T>(bool IsSuccess, T? Data, string? ErrorMessage);
