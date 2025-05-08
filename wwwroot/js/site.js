// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", function () {
  // Handle dropdowns
  const dropdowns = document.querySelectorAll(".dropdown");
  let activeDropdown = null;

  dropdowns.forEach((dropdown) => {
    const toggle = dropdown.querySelector(".dropdown-toggle");
    const menu = dropdown.querySelector(".dropdown-menu");

    toggle.addEventListener("click", function (e) {
      e.preventDefault();
      e.stopPropagation();

      // Close other dropdowns
      if (activeDropdown && activeDropdown !== dropdown) {
        activeDropdown.querySelector(".dropdown-menu").classList.remove("show");
      }

      // Toggle current dropdown
      menu.classList.toggle("show");
      activeDropdown = menu.classList.contains("show") ? dropdown : null;
    });
  });

  // Close dropdowns when clicking outside
  document.addEventListener("click", function (e) {
    if (!e.target.closest(".dropdown") && activeDropdown) {
      activeDropdown.querySelector(".dropdown-menu").classList.remove("show");
      activeDropdown = null;
    }
  });

  // Handle mobile menu toggle
  const navbarToggler = document.querySelector(".navbar-toggler");
  const navbarCollapse = document.querySelector(".navbar-collapse");

  if (navbarToggler && navbarCollapse) {
    navbarToggler.addEventListener("click", function () {
      navbarCollapse.classList.toggle("show");
    });

    // Close mobile menu when clicking outside
    document.addEventListener("click", function (e) {
      if (
        !e.target.closest(".navbar") &&
        navbarCollapse.classList.contains("show")
      ) {
        navbarCollapse.classList.remove("show");
      }
    });
  }

  // Close mobile menu when selecting an item
  const navItems = document.querySelectorAll(".navbar-nav .nav-link");
  navItems.forEach((item) => {
    item.addEventListener("click", function () {
      if (
        window.innerWidth < 992 &&
        !this.classList.contains("dropdown-toggle")
      ) {
        navbarCollapse.classList.remove("show");
      }
    });
  });

  // Handle inventory forms
  const productForms = document.querySelectorAll("#productForm");
  productForms.forEach((form) => {
    form.addEventListener("submit", function (e) {
      const quantityInput = this.querySelector('input[name="Quantity"]');
      const quantity = parseInt(quantityInput.value);
      const statusSelect = this.querySelector('select[name="Status"]');

      // Automatically update status based on quantity
      if (quantity <= 0) {
        statusSelect.value = "OUT_OF_STOCK";
      } else if (quantity < 10) {
        // Keep current status but maybe show a warning
        const warningThreshold =
          this.querySelector('select[name="ProductType"]').value === "FOOD"
            ? 5
            : 2;
        if (quantity <= warningThreshold) {
          alert("Cảnh báo: Số lượng sản phẩm thấp!");
        }
      }
    });
  });

  // Handle table type change in table management
  const tableTypeSelects = document.querySelectorAll(
    'select[name="tableType"]'
  );
  tableTypeSelects.forEach((select) => {
    select.addEventListener("change", function () {
      const price = this.value === "VIP" ? 200000 : 100000;
      const row = this.closest("tr");
      if (row) {
        const priceCell = row.querySelector("td:nth-child(4)");
        if (priceCell) {
          priceCell.textContent = price.toLocaleString("vi-VN") + " đ";
        }
      }
    });
  });
});
