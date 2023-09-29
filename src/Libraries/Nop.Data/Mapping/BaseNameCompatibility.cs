using System;
using System.Collections.Generic;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Security;

namespace Nop.Data.Mapping
{
    /// <summary>
    /// Base instance of backward compatibility of table naming
    /// </summary>
    public partial class BaseNameCompatibility : INameCompatibility
    {
        public Dictionary<Type, string> TableNames => new()
        {
            { typeof(CustomerCustomerRoleMapping), "Customer_CustomerRole_Mapping" },
            { typeof(PermissionRecordCustomerRoleMapping), "PermissionRecord_Role_Mapping" },
        };

        public Dictionary<(Type, string), string> ColumnName => new()
        {
            { (typeof(CustomerCustomerRoleMapping), "CustomerId"), "Customer_Id" },
            { (typeof(CustomerCustomerRoleMapping), "CustomerRoleId"), "CustomerRole_Id" },
            { (typeof(PermissionRecordCustomerRoleMapping), "PermissionRecordId"), "PermissionRecord_Id" },
            { (typeof(PermissionRecordCustomerRoleMapping), "CustomerRoleId"), "CustomerRole_Id" },
        };
    }
}