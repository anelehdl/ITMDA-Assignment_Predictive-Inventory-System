using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class RefreshToken       //not working fully         --TESTING UPDATE
    {
        [BsonElement("token_id")]
        public string? TokenId { get; set; }      // serverside id for the token

        [BsonElement("token_hash")]
        public string? TokenHash { get; set; }    // hashed token value

        [BsonElement("expires_at")]
        public DateTime ExpiresAt { get; set; }   // expiration date

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; }   // creation date

        //need to add replacement id

        [BsonElement("replaced_by_token_id")]
        public string? ReplacedByTokenId { get; set; }  // id of the token that replaced this one

        //also for tracking of revoked tokens
        [BsonElement("revoked_at")]
        public DateTime? RevokedAt { get; set; }  // date when the token was revoked

    }
}
