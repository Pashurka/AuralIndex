namespace AuralIndex.Application;

public class AlgorithmWeightsDto
{
    public double SearchTitleExact { get; set; } = 60;
    public double SearchTitleStartsWith { get; set; } = 45;
    public double SearchTitleContains { get; set; } = 30;
    public double SearchAuthorExact { get; set; } = 35;
    public double SearchGenreExact { get; set; } = 25;
    public double SearchLanguageExact { get; set; } = 20;
    public double RecommendationGenre { get; set; } = 45;
    public double RecommendationLanguage { get; set; } = 20;
    public double RecommendationAuthor { get; set; } = 25;
    public double RecommendationYearNear { get; set; } = 10;
    public double RecommendationYearMedium { get; set; } = 5;
    public double RecommendationDurationNear { get; set; } = 10;
    public double RecommendationDurationMedium { get; set; } = 5;
    public double PersonalizedFavoriteBoost { get; set; } = 3.5;
    public double PersonalizedViewStartBoost { get; set; } = 2.5;
    public double PersonalizedGenrePreference { get; set; } = 6;
    public double PersonalizedLanguagePreference { get; set; } = 4;
    public double PersonalizedAuthorPreference { get; set; } = 5;
}
