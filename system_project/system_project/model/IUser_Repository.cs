using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace system_project.model
{
    public interface IUser_Repository
    {
        bool Authenticate_User(NetworkCredential credential);
        void Add(User_Model user_Model);
        void Edit(User_Model user_Model);
        void Remove(User_Model user_Model);
        User_Model GetbyId(int id);
        User_Model GetByUsername(string username);
        IEnumerable<User_Model> GetAll();
        // ....
    }
}
