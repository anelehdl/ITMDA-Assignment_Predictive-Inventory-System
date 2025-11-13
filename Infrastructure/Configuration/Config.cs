public class MongoDBSettings
{
    public required string ConnectionString { get; set; }
    public required string DatabaseName { get; set; }
    public required string ForecastDatabaseName { get; set; }
    public required CollectionNames CollectionNames { get; set; }
    public required ForecastCollectionNames ForecastCollectionNames { get; set; }
}

public class CollectionNames
{
    public string Staff { get; set; }
    public string Roles { get; set; }
    public string Authentication { get; set; }
    public string Client { get; set; }
}

public class ForecastCollectionNames
{
    public string Inventory { get; set; }
    public string BatchScans { get; set; }
    public string ForecastCache { get; set; }
    public string Prediction { get; set; }
    public string Orders { get; set; }
}

public class JwtSettings
{
    public string SecretKey { get; set; }       //token
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public int ExpiryMinutes { get; set; }

    public int RefreshTokenExpiryDays { get; set; } //refresh token expiration in days

}