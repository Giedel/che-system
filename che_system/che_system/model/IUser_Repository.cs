using System;
using System.Collections.Generic;
using System.Net;

namespace che_system.model
{
    public interface IUser_Repository
    {
        bool Authenticate_User(NetworkCredential credential);
        void Add(User_Model user_Model);
        void Edit(User_Model user_Model);
        void Remove(User_Model user_Model);
        User_Model? GetbyId(int id);
        User_Model? GetByUsername(string username);
        IEnumerable<User_Model> GetAll();
        // ....
    }
}
