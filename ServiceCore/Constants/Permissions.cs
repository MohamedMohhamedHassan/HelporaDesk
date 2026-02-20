using System.Collections.Generic;

namespace ServiceCore.Constants
{
    public static class Permissions
    {
        // Dashboard
        public const string Dashboard_View = "Dashboard_View";

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
        public const string Assets_Create = "Assets_Create";
        public const string Assets_Edit = "Assets_Edit";
        public const string Assets_Delete = "Assets_Delete";
        public const string Assets_Manage = "Assets_Manage";

        // Users
        public const string Users_View = "Users_View";
        public const string Users_Create = "Users_Create";
        public const string Users_Edit = "Users_Edit";
        public const string Users_Delete = "Users_Delete";
        public const string Users_Manage = "Users_Manage";

        // Admin
        public const string Admin_Access = "Admin_Access";
        public const string Admin_Permissions = "Admin_Permissions";
        public const string Admin_Settings = "Admin_Settings";

        // Solutions
        public const string Solutions_View = "Solutions_View";
        public const string Solutions_Create = "Solutions_Create";
        public const string Solutions_Edit = "Solutions_Edit";
        public const string Solutions_Delete = "Solutions_Delete";
        public const string Solutions_Manage = "Solutions_Manage";
        
        // Contracts
        public const string Contracts_View = "Contracts_View";
        public const string Contracts_Create = "Contracts_Create";
        public const string Contracts_Edit = "Contracts_Edit";
        public const string Contracts_Delete = "Contracts_Delete";
        public const string Contracts_Manage = "Contracts_Manage";

        // Kanban
        public const string Kanban_View = "Kanban_View";
        public const string Kanban_Move = "Kanban_Move";

        // Reports
        public const string Reports_View = "Reports_View";

        // Approvals
        public const string Approvals_View = "Approvals_View";
        public const string Approvals_Manage = "Approvals_Manage";

        // Help
        public const string Help_Usage = "Help_Usage";

        // Tasks
        public const string Tasks_View = "Tasks_View";
        public const string Tasks_Create = "Tasks_Create";
        public const string Tasks_Edit = "Tasks_Edit";
        public const string Tasks_Delete = "Tasks_Delete";
        public const string Tasks_Manage = "Tasks_Manage";

        public static readonly List<string> All = new List<string>
        {
            Dashboard_View,
            Tickets_View, Tickets_Create, Tickets_Edit, Tickets_Delete, Tickets_Manage,
            Projects_View, Projects_Create, Projects_Edit, Projects_Manage,
            Assets_View, Assets_Create, Assets_Edit, Assets_Delete, Assets_Manage,
            Users_View, Users_Create, Users_Edit, Users_Delete, Users_Manage,
            Admin_Access, Admin_Permissions, Admin_Settings,
            Solutions_View, Solutions_Create, Solutions_Edit, Solutions_Delete, Solutions_Manage,
            Contracts_View, Contracts_Create, Contracts_Edit, Contracts_Delete, Contracts_Manage,
            Kanban_View, Kanban_Move,
            Reports_View,
            Approvals_View, Approvals_Manage,
            Help_Usage,
            Tasks_View, Tasks_Create, Tasks_Edit, Tasks_Delete, Tasks_Manage
        };
    }
}
