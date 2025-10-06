//-- User_Model.cs --

namespace che_system.model
{
    public class User_Model
    {
        public string user_id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string birthday { get; set; }
        public string role { get; set; } // "STA" or "Custodian"
    }
}
