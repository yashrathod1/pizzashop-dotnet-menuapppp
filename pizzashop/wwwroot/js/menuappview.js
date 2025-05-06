
$(document).ready(function () {

    $('#ModifierList').on('hidden.bs.modal', function () {
        $('.modifier-box').removeClass('selected');
        selectedModifiers = {}
    });

    var orderId = $('#orderId').val();

    LoadItems("all")
    fetchTableInfo(orderId);
    if (orderId > 0) {
        LoadOrderBasedOnStatus(orderId);
    }

    $('.category-item').on('click', function () {
        let type = $(this).data('type');
        let categoryId = $(this).data('category-id');

        $('.category-item').removeClass('selected text-white').addClass('text-muted');
        $(this).addClass('selected text-white').removeClass('text-muted');
        LoadItems(type, categoryId)

    });

    $('#searchOrders').on('input', function () {
        let searchTerm = $(this).val();
        let selectedCategory = $('.category-item.selected').data('category-id');
        let selectedType = $('.category-item.selected').data('type') || 'all';

        LoadItems(selectedType, selectedCategory, searchTerm);
    });
});

function LoadOrderBasedOnStatus(orderId)
{
        $.ajax({
            url: '/MenuApp/GetOrderStatus',
            type: 'GET',
            data: { orderId: orderId },
            success: function (response) {
                if (response.status === "Completed") {
                    loadOrderDetails(orderId);
                    $('#saveBtn, #completeBtn, #cancelBtn').hide();
                } else if (response.status === "Pending" || response.status === "In Progress"){
                    loadOrderDetails(orderId);
                    $('#saveBtn, #completeBtn, #cancelBtn').show();
                }
            },
            error: function () {
                toastr.error("Failed to fetch order status.");
            }
        });    
}

function LoadItems(type, categoryId, searchTerm = '') {
    $.ajax({
        url: '/MenuApp/GetItems',
        type: 'GET',
        data: { type: type, categoryId: categoryId, searchTerm: searchTerm },
        success: function (result) {
            $('#ItemsCardContainer').html(result);
        },
        error: function () {
            $('#ItemsCardContainer').html('<div class="text-danger fs-5">Failed to load items.</div>');
        }
    });
}


$(document).on('click', '.favourite', function () {
    const icon = $(this);
    const itemId = icon.data('itemid');

    $.ajax({
        url: '/MenuApp/ToggleIsFavourite',
        type: 'POST',
        data: { id: itemId },
        success: function (isFavourite) {
            if (isFavourite) {
                icon.removeClass('fa-regular text-secondary')
                    .addClass('fa-solid heart-color');

            } else {
                icon.removeClass('fa-solid heart-color')
                    .addClass('fa-regular text-secondary');
            }
        },
        error: function () {
            toastr.error("Failed to toggle favourite.");
        }
    });
});

// modifier in item card


$(document).on('click', '.item-card', function () {

    const itemId = $(this).data("id");

    selectedModifiers = {};

    $.ajax({
        url: '/MenuApp/GetModifierInItemCard',
        type: 'GET',
        data: { id: itemId },
        success: function (result) {
            $('#modifierDetailContainer').html(result);
            $('#ModifiersList').modal('show');
            checkMinSelection();
        },
        error: function () {
            toastr.error("Error loading Modifier");
        }
    });
});

//fetch order and table i--nfo
function fetchTableInfo(orderId) {
    $.ajax({
        url: '/MenuApp/GetTableDetailsByOrderId',
        type: 'GET',
        data: { orderId: orderId },
        success: function (response) {
            $('#sectionName').text(response.floorName);
            $('#assignedTables').text(response.assignedTables);
        },
        error: function () {
            alert('Error fetching table information.');
        }
    });
}


//modifiermodal
$(document).on('click', '.modifier-box', function () {
    var groupDiv = $(this).closest('.mb-3');
    var modifierGroupId = groupDiv.data('modifier-group-id');
    var modifierId = $(this).data('modifier-id');
    var modifierName = $(this).data('modifier-name');
    var modifierQty = parseInt($(this).data('modifier-qty'));
    var maxCount = parseInt(groupDiv.data('max-quantity')) || 0;

    // Check existing usage of this modifier
    // var totalUsedQty = 0;
    // $('.order-row').each(function () {
    //     var itemId = $('#itemTitle').data('item-id');
    //     if ($(this).data('item-id') == itemId) {
    //         var qty = parseInt($(this).find('.value').text());
    //         var modIds = $(this).data('modifier-ids').toString().split(',');

    //         if (modIds.includes(modifierId.toString())) {
    //             totalUsedQty += qty;
    //         }
    //     }
    // });

    // if (totalUsedQty >= modifierQty) {
    //     toastr.warning(`Modifier '${modifierName}' has already been used with quantity ${totalUsedQty}. Max allowed is ${modifierQty}.`);
    //     return;
    // }

    // Proceed with selection logic
    if (!selectedModifiers[modifierGroupId]) {
        selectedModifiers[modifierGroupId] = [];
    }

    var isSelected = $(this).hasClass('selected');

    if (!isSelected) {
        if (selectedModifiers[modifierGroupId].length < maxCount) {
            $(this).addClass('selected');
            selectedModifiers[modifierGroupId].push({ modifierId: modifierId });
        } else {
            toastr.warning(`Maximum limit is ${maxCount}`);
        }
    } else {
        $(this).removeClass('selected');
        selectedModifiers[modifierGroupId] = selectedModifiers[modifierGroupId].filter(function (mod) {
            return mod.modifierId !== modifierId;
        });
    }

    checkMinSelection();
});




function checkMinSelection() {
    var allGroupsSelected = true;

    $('.mb-3').each(function () {
        var minQuantity = parseInt($(this).data('min-quantity')) || 0;
        var maxQuantity = parseInt($(this).data('max-quantity')) || 0;
        var selectedCount = $(this).find('.modifier-box.selected').length;

        if (selectedCount < minQuantity || (maxQuantity > 0 && selectedCount > maxQuantity)) {
            allGroupsSelected = false;
        }
    });

    $('#AddToOrder').prop('disabled', !allGroupsSelected);
}

// add order of item and modifier
$(document).on('click', '#AddToOrder', function () {
    var itemId = $('#itemTitle').data('item-id');
    var availableQty = parseInt($('#itemTitle').data('available-qty'));
    var selectedModifierIds = [];

    var totalUsedQty = 0;

    $('.order-row').each(function () {
        if ($(this).data('item-id') == itemId) {
            var qty = parseInt($(this).find('.value').text());
            totalUsedQty += qty;
        }
    });

    if (totalUsedQty >= availableQty) {
        toastr.warning(`You cannot add more. Available quantity(${availableQty}) is already used.`);
        return;
    }

    for (var groupId in selectedModifiers) {
        selectedModifiers[groupId].forEach(function (mod) {
            selectedModifierIds.push(mod.modifierId);
        });
    }

    $.ajax({
        type: 'POST',
        url: '/MenuApp/AddOrderItemPartial',
        data: {
            ItemId: itemId,
            ModifierIds: selectedModifierIds
        },
        success: function (html) {
            $('#orderedItemsList').append(html);
            $('#ModifiersList').modal('hide');
            selectedModifiers = {};
            calculateAndRenderTaxSummary();
            $('#completeBtn').prop('disabled', true);

            // $('#saveBtn').prop('disabled', false);
        }
    });
});

// update item price as quantity increase and decrease

function updateAmounts($row, quantity) {
    var baseItemAmount = parseFloat($row.find('.item-amount').data('base-amount'));
    var baseModifierAmount = parseFloat($row.find('.totalmodifier-amount').data('base-amount'));

    var totalItemAmount = baseItemAmount * quantity;
    var totalModifierAmount = baseModifierAmount * quantity;

    $row.find('.item-amount').text('₹' + totalItemAmount.toFixed(2));
    $row.find('.totalmodifier-amount').text('₹' + totalModifierAmount.toFixed(2));
}

$(document).on('click', '.positive', function () {
    var $valueSpan = $(this).siblings('.value');
    var currentQty = parseInt($valueSpan.text());
    var $row = $(this).closest('tr');
    var itemId = $row.data('item-id');
    var availableQty = parseInt($row.data('available-qty'));

    var totalUsedQty = 0;

    $('.order-row').each(function () {
        if ($(this).data('item-id') == itemId) {
            totalUsedQty += parseInt($(this).find('.value').text());
        }
    });

    if (totalUsedQty < availableQty) {
        currentQty++;
        $valueSpan.text(currentQty);
        updateAmounts($row, currentQty);
        calculateAndRenderTaxSummary();
    } else {
        toastr.warning(`You cannot select more quantity than available quantity ${availableQty}!`);
    }
});



$(document).on('click', '.negative', function () {
    var $valueSpan = $(this).siblings('.value');
    var quantity = parseInt($valueSpan.text());
    if (quantity > 1) {
        quantity--;
        $valueSpan.text(quantity);
        var $row = $(this).closest('tr');
        updateAmounts($row, quantity);
        calculateAndRenderTaxSummary();
    }
});


// $(document).on('click', '.delete-item', function () {
//     $(this).closest('tr').remove();
//     calculateAndRenderTaxSummary();

//     // $('#saveBtn, #completeBtn, #invoiceBtn').prop('disabled', true);
// });

function calculateSubTotal() {
    let subTotal = 0;

    $('#orderedItemsList .order-row').each(function () {
        let itemAmount = parseFloat($(this).find('.item-amount').data('base-amount'));
        let modifierAmount = parseFloat($(this).find('.totalmodifier-amount').data('base-amount'));
        let quantity = parseInt($(this).find('.value').text());

        if (!isNaN(itemAmount) && !isNaN(modifierAmount) && !isNaN(quantity)) {
            subTotal += (itemAmount + modifierAmount) * quantity;
        }
    });

    return subTotal;
}

function calculateAndRenderTaxSummary() {
    let subTotal = calculateSubTotal();
    let taxTotal = 0;

    $('#tax-summary .tax-amount').each(function () {
        let $this = $(this);
        let rate = parseFloat($this.data('rate'));
        let type = $this.data('type');
        // let isDefault = $this.data('default') === true || $this.data('default') === "true";

        let amount = 0;

        if (type === "Flat Amount") {
            amount = rate;
        } else if (type === "Percentage") {
            amount = (subTotal * rate) / 100;
        }

        $this.text('₹' + amount.toFixed(2));
        taxTotal += amount;
    });

    let grandTotal = subTotal + taxTotal;
    $('#sub-total').text('₹' + subTotal.toFixed(2));
    $('#tax-total').text('₹' + taxTotal.toFixed(2));
    $('#grand-total').text('₹' + grandTotal.toFixed(2));
}

// $(document).on('change', '.tax-checkbox', function () {
//     calculateAndRenderTaxSummary();
// });


function getOrderIdFromUrl() {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get('orderId');
}


$(document).on('click', '#saveBtn', function () {
    var orderId = parseInt(getOrderIdFromUrl());

    var paymentMethod = $('input[name="paymentMethod"]:checked').val();

    if (!paymentMethod) {
        toastr.warning("Please select a payment method.");
        return;
    }

    var orderData = {
        orderId: orderId,
        items: [],
        subtotal: 0,
        total: 0,
        taxes: [],
        paymentMethod: paymentMethod
    };
    $('.order-row').each(function () {
        var $row = $(this);
        var itemId = $row.data('item-id');
        var itemName = $row.find('.item-name').data('iname');
        var quantity = parseInt($row.find('.quantity-input').text()) || 1;
        var itemAmount = parseFloat($row.find('.item-amount').text().replace('₹', '')) || 0;
        var modifierAmount = parseFloat($row.find('.totalmodifier-amount').text().replace('₹', '')) || 0;

        var modifiers = [];
        $row.find('.accordion-body li').each(function () {
            var $mod = $(this);
            var modName = $mod.find('.modifier-name').data('mname');
            var modAmount = parseFloat($mod.find('.modifier-amount').data('mamount')) || 0;
            var modQuantity = parseFloat($mod.find('.modifier-info').data('modifier-qty')) || 0;
            var modId = parseFloat($mod.find('.modifier-info').data('modifier-id')) || 0;

            modifiers.push({
                Id: modId,
                Name: modName,
                Amount: modAmount,
                Quantity: modQuantity
            });
        });
        console.log("modifier for item", itemName, modifiers)

        orderData.items.push({
            Id: itemId,
            itemName: itemName,
            ItemQuantity: quantity,
            itemAmount: itemAmount,
            SelectedModifiers: modifiers
        });

        orderData.subtotal += (itemAmount + modifierAmount) * quantity;
    });

    $('.tax-row').each(function () {
        var $tax = $(this);
        var taxName = $tax.data('tax-name');
        var taxId = $tax.data('tax-id');
        var taxRate = parseFloat($tax.find('.tax-amount').data('rate')) || 0;
        var taxAmount = parseFloat($tax.find('.tax-amount').text().replace('₹', '')) || 0;

        orderData.taxes.push({
            Id: taxId,
            Name: taxName,
            Value: taxRate,
            Amount: taxAmount
        });

        orderData.total += taxAmount;
    });

    orderData.total += orderData.subtotal;

    console.log("data is", orderData);

    $.ajax({
        url: '/MenuApp/SaveOrder',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(orderData),
        success: function (response) {
            toastr.success('Order saved!');
            $('#completeBtn').prop('disabled', false);
        },
        error: function () {
            toastr.error('Failed to save order.');
        }
    });
});

function loadOrderDetails(orderId) {

    $.ajax({
        url: '/MenuApp/GetOrderDetails',
        type: 'GET',
        data: { orderId: orderId },
        success: function (response) {
            $("#orderDetailsContainer").html(response);
            calculateAndRenderTaxSummary();
            $('#completeBtn').prop('disabled', false);
        },
        error: function () {
            toastr.error('Failed to Get OrderDetails.');
        }
    });
}

// delete orderitem

$(document).on('click', '.delete-item', function () {
    const $row = $(this).closest('tr');
    const itemId = $row.data('item-id');

    const isTemporary = $row.data('is-temporary');

    if (isTemporary) {
        $row.remove();
        calculateAndRenderTaxSummary();
        return;
    }

    else {
        $.ajax({
            type: "POST",
            url: "/MenuApp/DeleteOrderItem",
            data: { itemId: itemId },
            success: function (response) {
                if (response.success) {
                    $row.remove();
                    toastr.success(response.message);
                    calculateAndRenderTaxSummary();
                } else {
                    toastr.error(response.message);
                }
            },
            error: function () {
                toastr.error("Something went wrong while deleting the item.");
            }
        });
    }

});

// get customer details 

$(document).on('click', '#CustomerDetails', function () {
    var orderId = $('#orderId').val();

    $.ajax({
        type: "GET",
        url: "/MenuApp/GetCustomerDetails",
        data: { orderId },
        success: function (customer) {
            if (customer) {
                $('#orderCustomerName').val(customer.name);
                $('#orderCustomerMobileNo').val(customer.mobileNo);
                $('#orderCustomerNoOfPerson').val(customer.noOfPerson);
                $('#orderCustomerEmail').val(customer.email);
                $('#customerId').val(customer.id);
                $('#OrderCustomerDetails').modal('show');
            }
        },
        error: function () {
            toastr.error("Failed to load customer details.");
        }

    });

});

// update customer details

$(document).on('click', '#editOrderCustomer', function () {

    var formData = {
        Id: $('#customerId').val(),
        Name: $('#orderCustomerName').val(),
        MobileNo: $('#orderCustomerMobileNo').val(),
        NoOfPerson: $('#orderCustomerNoOfPerson').val(),
        Email: $('#orderCustomerEmail').val()
    };

    $.ajax({
        type: "POST",
        url: "/MenuApp/UpdateCustomerDetails",
        data: formData,
        success: function (response) {
            if (response.success) {
                $('#OrderCustomerDetails').modal('hide');
                toastr.success(response.message);
            } else {
                toastr.error(response.message);
            }
        },
        error: function () {
            toastr.error("Something went wrong while updating the customer.");
        }
    });
});


// order comment

$(document).on('click', '#orderComment', function () {
    var orderId = $('#orderId').val();

    $.ajax({

        type: "GET",
        url: "/MenuApp/GetOrderCommentById",
        data: {orderId},
        success: function(order){
            if(order)
            {
                $('#orderCommentText').val(order.orderComment);
                $('#OrderComment').modal('show');
            }
        },
        error: function(){
            toastr.error("Error in Getting OrderComment")
        }
    });
});

$(document).on('click', '#saveOrderComment', function () {
    var orderId = $('#orderId').val();
    var formData = {
        OrderComment: $('#orderCommentText').val(),
        Id: orderId
    };

    $.ajax({
        type: "POST",
        url: "/MenuApp/UpdateComment",
        data: formData,
        success: function (response) {
            if (response.success) {
                $('#OrderComment').modal('hide');
                toastr.success(response.message);
            } else {
                toastr.error(response.message);
            }
        },
        error: function () {
            toastr.error("Something went wrong while updating the comment.");
        }
    });
});

// item specific comment

$(document).on('click', '.order-row', function(){
    $('#SpecialInstruction').modal('show');
});

// complete order

$(document).on('click', '#completeBtn', function () {
    var orderId = parseInt(getOrderIdFromUrl());

    $.ajax({
        type: 'POST',
        url: '/MenuApp/CompleteOrder',
        data: { orderId: orderId },
        success: function (response) {
            if (response.success) {
                toastr.success(response.message);
                $('#completeBtn').prop('disabled', true);
            } else {
                toastr.error(response.message);
            }
        },
        error: function () {
            toastr.error("An unexpected error occurred.");
        }
    });
});
