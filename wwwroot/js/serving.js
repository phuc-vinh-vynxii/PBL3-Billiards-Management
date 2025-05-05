// Calculate total amount for billiards table and services
function calculateTotal() {
  // Calculate playing time
  const startTime = document.getElementById("startTime").value;
  const endTime = document.getElementById("endTime").value;

  const start = new Date(`2025-01-01 ${startTime}`);
  const end = new Date(`2025-01-01 ${endTime}`);
  const diffHours = (end - start) / (1000 * 60 * 60);

  // Table price per hour
  const pricePerHour = 100000;
  const tableTotal = diffHours * pricePerHour;

  // Calculate service total
  let serviceTotal = 0;
  const rows = document.querySelectorAll("#serviceTableBody tr");
  rows.forEach((row) => {
    const quantity = parseInt(row.querySelector(".quantity-input").value);
    const price = parseInt(
      row.querySelector("td:nth-child(3)").textContent.replace(/\D/g, "")
    );
    const total = quantity * price;
    row.querySelector("td:nth-child(4)").textContent =
      total.toLocaleString() + "";
    serviceTotal += total;
  });

  // Update display
  document.getElementById("playTime").textContent = `${diffHours} giờ`;
  document.getElementById(
    "tablePrice"
  ).textContent = `${tableTotal.toLocaleString()} VND`;
  document.getElementById(
    "servicePrice"
  ).textContent = `${serviceTotal.toLocaleString()} VND`;
  document.getElementById("totalPrice").textContent = `${(
    tableTotal + serviceTotal
  ).toLocaleString()} VND`;
}

// Handle product selection changes
function handleProductSelection() {
  const form = document.querySelector("#addOrderForm");
  const productSelect = form.querySelector('select[name="productId"]');
  const quantityInput = form.querySelector('input[name="quantity"]');

  productSelect.addEventListener("change", () => updateProductPreview(form));
  quantityInput.addEventListener("input", () => updateProductPreview(form));

  // Handle form submission
  form.addEventListener("submit", async function (e) {
    e.preventDefault();

    try {
      const formData = new FormData(form);
      const token = document.querySelector(
        'input[name="__RequestVerificationToken"]'
      ).value;

      const response = await fetch(form.action, {
        method: "POST",
        headers: {
          RequestVerificationToken: token,
        },
        body: formData,
      });

      const result = await response.json();

      if (result.success) {
        // Add new row to table
        const tbody = document.getElementById("orderTableBody");
        const newRow = document.createElement("tr");
        newRow.innerHTML = `
                    <td>${result.productName}</td>
                    <td>${result.quantity}</td>
                    <td class="text-end">${result.price.toLocaleString(
                      "vi-VN"
                    )} đ</td>
                    <td class="text-end order-total">${result.total.toLocaleString(
                      "vi-VN"
                    )} đ</td>
                `;
        tbody.appendChild(newRow);

        // Update totals
        updateOrderTotal();

        // Close modal
        const modalElement = document.getElementById("addServiceModal");
        const modal = bootstrap.Modal.getInstance(modalElement);
        modal.hide();

        // Reset form
        form.reset();

        // Clear preview
        document.getElementById("productPreview").innerHTML = "";
      } else {
        alert(result.message || "Có lỗi xảy ra khi thêm sản phẩm");
      }
    } catch (error) {
      console.error("Error:", error);
      alert("Có lỗi xảy ra khi thêm sản phẩm. Vui lòng thử lại.");
    }
  });
}

// Update the preview of selected product
function updateProductPreview(form) {
  const productSelect = form.querySelector('select[name="productId"]');
  const quantityInput = form.querySelector('input[name="quantity"]');
  const selectedOption = productSelect.options[productSelect.selectedIndex];

  if (selectedOption.value) {
    const price = parseFloat(selectedOption.getAttribute("data-price"));
    const quantity = parseInt(quantityInput.value) || 0;
    const total = price * quantity;

    // Update preview
    const previewElement = document.getElementById("productPreview");
    if (previewElement) {
      previewElement.innerHTML = `
                <div class="alert alert-info">
                    <div class="d-flex justify-content-between align-items-center">
                        <div>
                            <strong>${
                              selectedOption.text.split(" - ")[0]
                            }</strong><br>
                            <small>Số lượng: ${quantity}</small>
                        </div>
                        <div class="text-end">
                            <strong>${total.toLocaleString("vi-VN")}đ</strong>
                        </div>
                    </div>
                </div>
            `;
    }
  }
}

// Calculate total of all orders
function updateOrderTotal() {
  const orderTotals = document.querySelectorAll(".order-total");
  let total = 0;

  orderTotals.forEach((cell) => {
    const amount = parseFloat(cell.textContent.replace(/[^\d]/g, ""));
    total += amount;
  });

  // Update total display
  const totalElement = document.getElementById("orderTotalAmount");
  if (totalElement) {
    totalElement.textContent = `${total.toLocaleString("vi-VN")} đ`;
  }

  // Update invoice button total
  const tableTotal = parseFloat(
    document.getElementById("tableTotal").textContent.replace(/[^\d]/g, "")
  );
  const grandTotal = total + tableTotal;

  const invoiceButton = document.querySelector(
    'form[asp-action="GenerateInvoice"] button'
  );
  if (invoiceButton) {
    invoiceButton.innerHTML = `
            <i class="bi bi-receipt me-2"></i>
            Xuất hóa đơn (${grandTotal.toLocaleString("vi-VN")} đ)
        `;
  }
}

// Initialize on page load
document.addEventListener("DOMContentLoaded", function () {
  handleProductSelection();
});
