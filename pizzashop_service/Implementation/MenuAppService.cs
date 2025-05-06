using Microsoft.AspNetCore.Server.IIS.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Org.BouncyCastle.Math.EC.Rfc7748;
using pizzashop_repository.Interface;
using pizzashop_repository.Models;
using pizzashop_repository.ViewModels;
using pizzashop_service.Interface;

namespace pizzashop_service.Implementation;

public class MenuAppService : IMenuAppService
{
    private readonly IMenuAppRepository _menuAppRepository;

    public MenuAppService(IMenuAppRepository menuAppRepository)
    {
        _menuAppRepository = menuAppRepository;
    }

    public async Task<MenuAppViewModel> GetCategoriesAsync()
    {
        var categories = await _menuAppRepository.GetCategoriesAsync();

        var model = new MenuAppViewModel
        {
            CategoryList = categories.Select(c => new CategoryViewModel
            {
                Id = c.Id,
                Name = c.Name
            }).ToList(),
        };

        // if (orderId != 0)
        // {
        //     var orderItems = await _menuAppRepository.GetOrderItemsAsync(orderId);

        //     foreach (var item in orderItems)
        //     {
        //         var itemVm = new MenuAppAddItemViewModel
        //         {
        //             Id = item.Menuitemid,
        //             ItemName = item.ItemName,
        //             ItemAmount = item.Price,
        //             ItemQuantity = item.Quantity,
        //             SelectedModifiers = new List<MenuAppAddModifierViewModel>()
        //         };


        //         var modifiers = await _menuAppRepository.GetOrderModifiersAsync(item.Id);

        //         foreach (var mod in modifiers)
        //         {
        //             itemVm.SelectedModifiers.Add(new MenuAppAddModifierViewModel
        //             {
        //                 Id = mod.Modifierid,
        //                 Name = mod.ModifierName,
        //                 Amount = mod.Price,
        //                 Quantity = mod.Quantity
        //             });
        //         }

        //         model.Items.Add(itemVm);
        //     }

        //     var taxes = await _menuAppRepository.GetOrderTaxesAsync(orderId);
        //     model.Taxes = taxes.Select(t => new MenuAppOrderTaxSummaryViewModel
        //     {
        //         Id = t.TaxId,
        //         Name = t.TaxName,
        //         Value = t.TaxValue,
        //         Amount = t.TaxAmount
        //     }).ToList();
        // }

        return model;


    }


    public async Task<List<MenuAppItemViewModel>> GetItemsAsync(int? categoryId = null, bool? isFavourite = null, string? searchTerm = null)
    {
        IQueryable<MenuItem> query = _menuAppRepository.GetAllItemsQuery();

        if (categoryId.HasValue)
        {
            query = query.Where(m => m.Categoryid == categoryId.Value);
        }

        if (isFavourite.HasValue)
        {
            query = query.Where(m => m.Isfavourite == isFavourite.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string lowerSearch = searchTerm.ToLower();
            query = query.Where(m => m.Name.ToLower().Contains(lowerSearch));
        }

        var items = await query.ToListAsync();

        return items.Select(item => new MenuAppItemViewModel
        {
            Id = item.Id,
            Name = item.Name,
            ItemType = item.Type,
            IsFavourite = item.Isfavourite,
            Rate = item.Rate,
            ItemImagePath = item.ItemImage
        }).ToList();
    }



    public async Task<bool> ToggleIsFavourite(int id)
    {
        try
        {
            var items = await _menuAppRepository.GetItemById(id);
            if (items == null) return false;

            items.Isfavourite = !items.Isfavourite;

            await _menuAppRepository.UpdateItemAsync(items);

            return items.Isfavourite;
        }
        catch (Exception ex)
        {
            throw new Exception("Error in Fetching item by category", ex);
        }
    }

    public async Task<MenuAppModifierDetailViewModel> GetModifierInItemCardAsync(int id)
    {
        try
        {
            var item = await _menuAppRepository.GetItemById(id);
            if (item == null)
                throw new Exception("Item Not Found");

            var mapping = await _menuAppRepository.GetModifierInItemCardAsync(id);

            var modifierGroups = mapping.Select(mapping => new MenuAppItemModifierGroupViewModel
            {

                ModifierGroupName = mapping.ModifierGroup.Name,
                ModifierGroupId = mapping.ModifierGroup.Id,
                MinQuantity = mapping.MinModifierCount,
                MaxQuantity = mapping.MaxModifierCount,
                Modifiers = mapping.ModifierGroup.Modifiergroupmodifiers.Where(mgm => !mgm.Isdeleted && !mgm.Modifier.Isdeleted)
                                                                    .Select(mgm => new MenuAppItemModifiersViewModel
                                                                    {
                                                                        Id = mgm.Modifier.Id,
                                                                        Name = mgm.Modifier.Name,
                                                                        Amount = mgm.Modifier.Price,
                                                                        Quantity = mgm.Modifier.Quantity
                                                                    }).ToList()


            }).ToList();

            return new MenuAppModifierDetailViewModel
            {
                ItemQuantity = item.Quantity,
                ItemId = item.Id,
                ItemName = item.Name,
                ModifierGroups = modifierGroups
            };

        }
        catch (Exception ex)
        {
            throw new Exception("Error getting modifier in item card", ex);
        }
    }

    public async Task<MenuAppTableSectionViewModel> GetTableDetailsByOrderIdAsync(int orderId)
    {
        var mappings = await _menuAppRepository.GetTableDetailsByOrderIdAsync(orderId);

        if (mappings == null || mappings.Count == 0)
            throw new Exception("No table mappings found for this order.");

        var sectionName = mappings.First().Table.Section.Name;

        var tableNames = mappings
            .Select(m => m.Table.Name)
            .Where(name => !string.IsNullOrEmpty(name))
            .Distinct()
            .ToList();

        return new MenuAppTableSectionViewModel
        {
            SectionName = sectionName,
            TableName = tableNames
        };
    }


    public async Task<MenuAppAddOrderItemViewModel> AddItemInOrder(int itemId, List<int> modifierIds)
    {
        var item = await _menuAppRepository.GetItemById(itemId) ?? throw new ArgumentException("Invalid item ID");

        var selectedModifiers = await _menuAppRepository.GetModifiersByIds(modifierIds);

        var totalModifierPrice = selectedModifiers.Sum(m => m.Price);

        var addItem = new MenuAppOrderItemViewModel
        {
            Id = item.Id,
            ItemName = item.Name,
            ItemAmount = item.Rate,
            ItemQuantity = item.Quantity,
            TotalModifierAmount = totalModifierPrice,
            SelectedModifiers = selectedModifiers.Select(m => new MenuAppModifierViewModel
            {
                Id = m.Id,
                Name = m.Name,
                Amount = m.Price,
                Quantity = m.Quantity
            }).ToList(),
        };
        return new MenuAppAddOrderItemViewModel
        {
            Items = new List<MenuAppOrderItemViewModel> { addItem }
        };
    }

    public async Task<MenuAppOrderDetailsViewModel> GetOrderDetailsAsync(int orderId)
    {
        var order = await _menuAppRepository.GetOrderById(orderId) ?? throw new ArgumentException("Invalid Order ID");
        var items = await _menuAppRepository.GetOrderItemsAsync(orderId);
        var modifiers = await _menuAppRepository.GetOrderModifiersAsync(orderId);
        var taxes = await _menuAppRepository.GetOrderTaxesAsync(orderId);

        var itemDetails = items.Select(i =>
        {

            var itemModifiers = modifiers
                .Where(m => m.Orderitemid == i.Id)
                .ToList();

            return new MenuAppOrderItemViewModel
            {
                Id = i.Id,
                ItemName = i.ItemName,
                ItemQuantity = i.Quantity,
                ItemAmount = i.TotalPrice,
                SelectedModifiers = itemModifiers.Select(m => new MenuAppModifierViewModel
                {
                    Id = m.Id,
                    Name = m.ModifierName,
                    Amount = m.Price,
                    Quantity = m.Quantity
                }).ToList(),

                TotalModifierAmount = itemModifiers.Sum(m => m.Price)
            };
        }).ToList();

        var taxDetails = taxes.Select(t => new MenuAppOrderTaxSummaryViewModel
        {
            Id = t.Id,
            Name = t.TaxName,
            TaxId = t.Tax.Id,
            Value = t.TaxValue,
            IsEnable = t.Tax.Isenabled,
            IsDefault = t.Tax.Isdefault,
            Type = t.Tax.Type
        }).ToList();

        return new MenuAppOrderDetailsViewModel
        {
            OrderId = orderId,
            Subtotal = order.Subamount,
            Items = itemDetails,
            Taxes = taxDetails,
            Total = order.Totalamount,
            PaymentMethod = order.Payment?.PaymentMethod ?? "",
        };
    }


    public async Task<bool> SaveOrder(MenuAppOrderDetailsViewModel model)
    {
        try
        {
            var order = await _menuAppRepository.GetOrderById(model.OrderId);
            if (order == null)
                return false;

            var existingPayment = await _menuAppRepository.GetPaymentByOrderIdAsync(model.OrderId);
            if (existingPayment == null)
            {
                var payment = new Payment
                {
                    Orderid = model.OrderId,
                    PaymentMethod = model.PaymentMethod,
                    Amount = model.Total
                };
                await _menuAppRepository.InsertPaymentInfoAsync(payment);
                order.Paymentid = payment.Id;
            }
            else if (existingPayment.PaymentMethod != model.PaymentMethod || existingPayment.Amount != model.Total)
            {
                existingPayment.PaymentMethod = model.PaymentMethod;
                existingPayment.Amount = model.Total;
                await _menuAppRepository.UpdatePaymentInfoAsync(existingPayment);
            }

            // Update order info
            order.Subamount = model.Subtotal;
            order.Totalamount = model.Total;
            order.Updatedat = DateTime.Now;
            order.Status = "In Progress";

            await _menuAppRepository.UpdateOrderAsync(order);

            foreach (var itemVm in model.Items)
            {
                var existingItem = await _menuAppRepository.GetOrderItemByIdAndOrderIdAsync(itemVm.Id, model.OrderId);

                OrderItemsMapping orderItem;
                if (existingItem != null &&
                    (existingItem.Quantity != itemVm.ItemQuantity || existingItem.TotalPrice != itemVm.ItemAmount))
                {
                    existingItem.Quantity += itemVm.ItemQuantity;
                    existingItem.TotalPrice = existingItem.Quantity * existingItem.Price;
                    await _menuAppRepository.UpdateOrderItemAsync(existingItem);

                    orderItem = existingItem;
                }
                else if (existingItem == null)
                {
                    orderItem = new OrderItemsMapping
                    {
                        Orderid = model.OrderId,
                        ItemName = itemVm.ItemName,
                        Menuitemid = itemVm.Id,
                        Quantity = itemVm.ItemQuantity,
                        Price = itemVm.ItemAmount,
                        TotalPrice = itemVm.ItemQuantity * itemVm.ItemAmount
                    };
                    await _menuAppRepository.InsertOrderItemAsync(orderItem);

                    int orderItemId = orderItem.Id;

                    foreach (var modVm in itemVm.SelectedModifiers)
                    {
                        var orderMod = new OrderItemModifier
                        {
                            Orderitemid = orderItemId,
                            ModifierName = modVm.Name,
                            Price = modVm.Amount,
                            Quantity = modVm.Quantity,
                            Modifierid = modVm.Id
                        };
                        await _menuAppRepository.InsertOrderModifierAsync(orderMod);
                    }

                    await _menuAppRepository.DecreaseItemQuantityAsync(itemVm.Id, itemVm.ItemQuantity);
                }
            }

            var tax = await _menuAppRepository.GetOrderTaxesAsync(model.OrderId);
            foreach (var taxVm in model.Taxes)
            {
                var existingTax = tax.FirstOrDefault(t => t.TaxId == taxVm.Id);
                if (existingTax != null)
                {
                    existingTax.TaxAmount = taxVm.Amount;
                    await _menuAppRepository.UpdateOrderTaxAsync(existingTax);
                }
            }

            var table = await _menuAppRepository.GetTableByOrderId(model.OrderId);
            if (table == null)
                return false;

            if (table.Status != "Occupied")
            {
                table.Status = "Occupied";
                await _menuAppRepository.UpdateTableAsync(table);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool Success, string Message)> DeleteOrderItemAsync(int orderItemId)
    {
        var orderItem = await _menuAppRepository.GetOrderItemByItemIdAsync(orderItemId);

        if (orderItem == null)
        {
            return (false, "Item not found");
        }

        if (orderItem.Preparedquantity > 0)
        {
            return (false, $"Item is prepared, you cannot delete this item ({orderItem.ItemName})");
        }

        orderItem.Isdeleted = true;

        var result = await _menuAppRepository.UpdateOrderItemAsync(orderItem);

        return result
            ? (true, "Item deleted successfully")
            : (false, "Failed to delete item");
    }

    public async Task<MenuAppCustomerViewModel?> GetCustomerDetailsByOrderId(int orderId)
    {
        var customerDetails = await _menuAppRepository.GetCustomerDetailsFromOrderId(orderId);
        if (customerDetails == null) return null;

        var model = new MenuAppCustomerViewModel
        {
            Id = customerDetails.Customer.Id,
            Name = customerDetails.Customer.Name,
            Email = customerDetails.Customer.Email,
            MobileNo = customerDetails.Customer.PhoneNumber,
            NoOfPerson = customerDetails.Customer.WaitingTokens.Select(wt => wt.NoOfPersons).FirstOrDefault()
        };

        return model;
    }

    public async Task<bool> UpdateCustomerDetailsAsync(MenuAppCustomerViewModel model)
    {
        var customer = await _menuAppRepository.GetCustomerByIdAsync(model.Id);
        if (customer == null) return false;

        customer.Name = model.Name;
        customer.PhoneNumber = model.MobileNo;
        customer.Email = model.Email;

        var waitingToken = customer.WaitingTokens.FirstOrDefault();
        if (waitingToken != null)
        {
            waitingToken.NoOfPersons = model.NoOfPerson;
        }

        return await _menuAppRepository.UpdateCustomerAsync(customer);
    }

    public async Task<MenuAppOrderViewModel?> GetOrderCommentById(int orderId)
    {
        var order = await _menuAppRepository.GetOrderById(orderId);
        if (order == null) return null;

        return new MenuAppOrderViewModel
        {
            OrderComment = order.Comment
        };
    }

    public async Task<bool> UpdateOrderComment(MenuAppOrderViewModel model)
    {
        var order = await _menuAppRepository.GetOrderById(model.Id);
        if (order == null) return false;

        order.Comment = model.OrderComment;

        return await _menuAppRepository.UpdateOrderAsync(order);
    }

    public async Task<(bool Success, string Message)> CompleteOrderAsync(int orderId)
    {
        var order = await _menuAppRepository.GetOrderById(orderId);
        if (order == null)
            return (false, "Order not found");

        // Get order items
        var orderItems = await _menuAppRepository.GetOrderItemListByOrderIdAsync(orderId);
        if (orderItems == null || orderItems.Count == 0)
            return (false, "No items found for this order");

        var notReadyItems = orderItems.Where(x => x.Preparedquantity < x.Quantity).ToList();
        if (notReadyItems.Any())
            return (false, "All items must be served before completing the orders");

        order.Status = "Completed";
        await _menuAppRepository.UpdateOrderAsync(order);

        var table = await _menuAppRepository.GetTableByOrderId(orderId);
        if (table != null)
        {
            table.Status = "Available";
            await _menuAppRepository.UpdateTableAsync(table);
        }

        var payment = await _menuAppRepository.GetPaymentByOrderIdAsync(orderId);
        if (payment != null)
        {
            payment.PaymentStatus = true;
            await _menuAppRepository.UpdatePaymentInfoAsync(payment);
        }

        return (true, "Order completed successfully");
    }

    public async Task<MenuAppOrderViewModel?> GetOrderStatusAsync(int orderId)
    {
        var order = await _menuAppRepository.GetOrderById(orderId);
        if (order == null) return null;

        return new MenuAppOrderViewModel
        {
            Status = order.Status
        };
    }



}

