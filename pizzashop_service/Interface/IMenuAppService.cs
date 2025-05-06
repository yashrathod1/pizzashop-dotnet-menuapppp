using pizzashop_repository.ViewModels;

namespace pizzashop_service.Interface;

public interface IMenuAppService
{
      Task<MenuAppViewModel> GetCategoriesAsync();

       Task<List<MenuAppItemViewModel>> GetItemsAsync(int? categoryId = null, bool? isFavourite = null, string? searchTerm = null);

      Task<bool> ToggleIsFavourite(int id);

      Task<MenuAppModifierDetailViewModel> GetModifierInItemCardAsync(int id);

      Task<MenuAppTableSectionViewModel> GetTableDetailsByOrderIdAsync(int orderId);

      Task<MenuAppAddOrderItemViewModel> AddItemInOrder(int itemId, List<int> modifierIds);

      Task<MenuAppOrderDetailsViewModel> GetOrderDetailsAsync(int orderId);

      Task<bool> SaveOrder(MenuAppOrderDetailsViewModel model);

      Task<(bool Success, string Message)> DeleteOrderItemAsync(int orderItemId);

      Task<MenuAppCustomerViewModel?> GetCustomerDetailsByOrderId(int orderId);

      Task<bool> UpdateCustomerDetailsAsync(MenuAppCustomerViewModel model);

      Task<MenuAppOrderViewModel?> GetOrderCommentById(int orderId);

      Task<bool> UpdateOrderComment(MenuAppOrderViewModel model);

      Task<(bool Success, string Message)> CompleteOrderAsync(int orderId);

      Task<MenuAppOrderViewModel?> GetOrderStatusAsync(int orderId);

}
