using WavesRP;

namespace TIDAL
{
    //! this namespace is used to deserialize the response from the Tidal API.
    //! its properties are the same as the response from the API. 
    public struct Albums_Attributes
    {
        public string title;
        public string type;
        public string barcodeId;
        public int numberOfVolumes;
        public int numberOfItems;
        public string duration;
        public string? releaseDate;
        public string? copyright;
        public double popularity;
        public bool explicit_content;
        public string[] availability;
        public string[] mediaTags;
        public External_Link[] externalLinks;
    }
    public struct Albums_Items_Multi_Data_Relationship_Document
    {
        public Albums_Items_Resource_Identifier[] data;
        public Links? links;
    }
    public struct Albums_Items_Resource_Identifier
    {
        public string id;
        public string type;
        public Albums_Items_Resource_Identifier_Meta? meta;
    }
    public struct Albums_Items_Resource_Identifier_Meta
    {
        public int volumeNumber;
        public int trackNumber;
    }
    public struct Albums_Relationships
    {
        public Multi_Data_Relationship_Doc artists;
        public Multi_Data_Relationship_Doc similarAlbums;
        public Multi_Data_Relationship_Doc coverArt;
        public Albums_Items_Multi_Data_Relationship_Document items;
        public Multi_Data_Relationship_Doc providers;
    }
    public struct Albums_Resource
    {
        public string id;
        public string type;
        public Albums_Attributes attributes;
        public Albums_Relationships relationships;
    }
    public struct Albums_Multi_Data_Document
    {
        public Albums_Resource[] data;
        public Links links;
        public Albums_Resource[] included;
    }
    public struct Artists_Attributes
    {
        public string name;
        public double popularity;
        public External_Link[] externalLinks;
        public string handle;
    }
    public struct Artists_Multi_Data_Document
    {
        public Artists_Resource[] data;
        public Links links;
        public Artists_Resource[] included;
    }
    public struct Artists_Relationships
    {
        public Multi_Data_Relationship_Doc similarArtists;
        public Multi_Data_Relationship_Doc albums;
        public Multi_Data_Relationship_Doc roles;
        public Multi_Data_Relationship_Doc videos;
        public Multi_Data_Relationship_Doc profileArt;
        public Artists_TrackProviders_Multi_Data_Relationship_Doc trackProviders;
        public Multi_Data_Relationship_Doc tracks;
        public Multi_Data_Relationship_Doc radio;
    }
    public struct Artists_Resource
    {
        public string id;
        public string type;
        public Artists_Attributes? attributes;
        public Artists_Relationships? relationships;
    }
    public struct Artists_TrackProviders_Multi_Data_Relationship_Doc
    {
        public Artists_TrackProviders_Resource_Identifier[] data;
        public Links? links;
    }
    public struct Artists_TrackProviders_Resource_Identifier
    {
        public string id;
        public string type;
        public Artists_TrackProviders_Resource_Identifier_Meta? meta;
    }
    public struct Artists_TrackProviders_Resource_Identifier_Meta
    {
        public long numberOfTracks;
    }
    public struct Artworks_Attributes
    {
        public string mediaType;
        public Artwork_File[] files;
    }
    public struct Artwork_File
    {
        public string href;
        public Artwork_File_Meta? meta;
    }
    public struct Artwork_File_Meta
    {
        public int width;
        public int height;
        public string? color;
    }
    public struct Artworks_Resource
    {
        public string id;
        public string type;
        public Artworks_Attributes attributes;
    }
    public struct Artworks_Single_Data_Document
    {
        public Artworks_Resource? data;
        public Links? links;
    }
    public struct ClientCredentialsResponse
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
    }
    public struct External_Link
    {
        public string href;
        public External_Link_Meta meta;
    }
    public struct External_Link_Meta
    {
        public string type;
    }
    public struct Links
    {
        public string self;
        public string next;
    }
    public struct Multi_Data_Relationship_Doc
    {
        public Resource_Identifier[] data;
        public Links links;
    }
    public struct Resource_Identifier
    {
        public string id;
        public string type;
    }
    public struct SearchResultResponse
    {
        public Resource_Identifier[] data;
        public Links links;
        public Tracks_Resource[] included;
    }
    public struct Tracks_Attributes
    {
        public string title;
        public string? version;
        public string isrc;
        public string duration;
        public string? copyright;
        public bool explicit_content;
        public double popularity;
        public string[] availability;
        public string[] mediaTags;
    }
    public struct Tracks_Relationships
    {
        public Multi_Data_Relationship_Doc artists;
        public Multi_Data_Relationship_Doc albums;
        public Multi_Data_Relationship_Doc providers;
        public Multi_Data_Relationship_Doc similarTracks;
        public Multi_Data_Relationship_Doc radio;
    }
    public struct Tracks_Resource
    {
        public string id;
        public string type;
        public Tracks_Attributes attributes;
        public Tracks_Relationships relationships;
    }
    public struct Tracks_Single_Data_Document
    {
        public Tracks_Resource? data;
        public Links? links;
        public Tracks_Resource[] included;
    }
}