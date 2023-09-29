using System.Collections.Generic;
using Nop.Web.Framework.Models;

namespace Nop.Web.Models.Customer
{
    public partial record CustomerNavigationModel : BaseNopModel
    {
        public CustomerNavigationModel()
        {
            CustomerNavigationItems = new List<CustomerNavigationItemModel>();
        }

        public IList<CustomerNavigationItemModel> CustomerNavigationItems { get; set; }

        public CustomerNavigationEnum SelectedTab { get; set; }
    }

    public record CustomerNavigationItemModel : BaseNopModel
    {
        public CustomerNavigationItemModel()
        {
            ChildCustomerNavigationItems = new List<CustomerNavigationItemModel>();
        }

        public string RouteName { get; set; }
        public string Link { get; set; }
        public string Title { get; set; }
        public CustomerNavigationEnum Tab { get; set; }
        public CustomerNavigationEnum SelectedTab { get; set; }
        public string ItemClass { get; set; }
        public IList<CustomerNavigationItemModel> ChildCustomerNavigationItems { get; set; }
    }

    public enum CustomerNavigationEnum
    {
        DashBoard = 0,
        Info = 1,
        Accounts = 2,
        Notes = 3,
        Addresses = 10,
        Orders = 20,
        RewardPoints = 60,
        ChangePassword = 70,
        Avatar = 80,
        ForumSubscriptions = 90,
        GdprTools = 120,
        MultiFactorAuthentication = 140
    }
}