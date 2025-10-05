using MongoDB.Bson;


//could segment this further by placing entities in an entity folder and dtos in dto folder
namespace PrototypeGroupProject_API.Models.Entities
{
    public class StaffEntity
    {
        //temp records for testing api and dashboard communication
        //will add more fields later on as needed when mongodb connection has been established
        public ObjectId Id { get; set; }        //mongodb uses object id as primary key
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // Store hashed password       //need to talk about this at next meeting
        //according to anelehs db design atm
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        //and also she has role
        public string Role { get; set; } = "Staff"; // Default role       can look at this later just want to get the dashboard communicating with the api first
        //so was thinking we can have roles like staff and admin, admin can add staff members and delete them etc via the api, using scalar?
        //can add a hidden button at bottom of dashboard to redirect to scalar ui
    }
}
