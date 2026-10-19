namespace Climato.Provider.Application.Model.Pexels;

public sealed record PexelsSearchResponse(IReadOnlyList<PexelsPhoto> Photos);

public sealed record PexelsPhoto(PexelsPhotoSource Src);

public sealed record PexelsPhotoSource(string Large2x);
