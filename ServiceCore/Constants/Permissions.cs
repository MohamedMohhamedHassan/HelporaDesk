using System.Collections.Generic;

namespace ServiceCore.Constants
{
    public static class Permissions
    {
        // Tickets
        public const string Tickets_View = "Tickets_View";
        public const string Tickets_Create = "Tickets_Create";
        public const string Tickets_Edit = "Tickets_Edit";
        public const string Tickets_Delete = "Tickets_Delete";
        public const string Tickets_Manage = "Tickets_Manage"; // General management

        // Projects
        public const string Projects_View = "Projects_View";
        public const string Projects_Create = "Projects_Create";
        public const string Projects_Edit = "Projects_Edit";
        public const string Projects_Manage = "Projects_Manage";

        // Assets
        public const string Assets_View = "Assets_View";
        public const string Assets_Manage = "Assets_Manage";

        // Users
        public const string Users_View = "Users_View";
        public const string Users_Manage = "Users_Manage";

        // Admin
        public const string Admin_Access = "Admin_Access";
        public const string Admin_Permissions = "Admin_Permissions";
        public const string Admin_Settings = "Admin_Settings";

        // Solutions
        public const string Solutions_View = "Solutions_View";
        public const string Solutions_Manage = "Solutions_Manage";
        
        // Contracts
        public const string Contracts_View = "Contracts_View";
        public const string Contracts_Manage = "Contracts_Manage";

        // Kanban
        public const string Kanban_View = "Kanban_View";
        public const string Kanban_Move = "Kanban_Move";

        public static readonly List<string> All = new List<string>
        {
            Tickets_View, Tickets_Create, Tickets_Edit, Tickets_Delete, Tickets_Manage,
            Projects_View, Projects_Create, Projects_Edit, Projects_Manage,
            Assets_View, Assets_Manage,
            Users_View, Users_Manage,
            Admin_Access, Admin_Permissions, Admin_Settings,
            Solutions_View, Solutions_Manage,
            Contracts_View, Contracts_Manage,
            Kanban_View, Kanban_Move
        };
    }
}
