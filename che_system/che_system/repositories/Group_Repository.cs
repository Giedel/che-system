//-- Group_Repository.cs --

using che_system.modals.model;
using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Data;

namespace che_system.repositories
{
    public class GroupRepository : Repository_Base
    {
        public ObservableCollection<GroupModel> GetAllGroups()
        {
            var groups = new ObservableCollection<GroupModel>();

            using var connection = GetConnection();
            string query = @"SELECT group_id, group_no FROM [Group] ORDER BY group_no";

            using var command = new SqlCommand(query, connection);
            connection.Open();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                groups.Add(new GroupModel
                {
                    GroupId = reader.GetInt32("group_id"),
                    GroupNo = reader.GetString("group_no")
                });
            }

            return groups;
        }

        public int AddGroup(GroupModel group)
        {
            using var connection = GetConnection();
            using var command = new SqlCommand(
                @"INSERT INTO [Group] (group_no)
                  VALUES (@group_no);
                  SELECT SCOPE_IDENTITY();", connection);

            command.Parameters.AddWithValue("@group_no", group.GroupNo);

            connection.Open();
            var id = command.ExecuteScalar();
            return Convert.ToInt32(id);
        }

        public GroupModel GetGroupById(int groupId)
        {
            using var connection = GetConnection();
            using var command = new SqlCommand(
                @"SELECT group_id, group_no
                  FROM [Group] WHERE group_id = @group_id", connection);
            command.Parameters.AddWithValue("@group_id", groupId);
            connection.Open();

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new GroupModel
                {
                    GroupId = reader.GetInt32("group_id"),
                    GroupNo = reader.GetString("group_no")
                };
            }

            return null;
        }
    }
}
