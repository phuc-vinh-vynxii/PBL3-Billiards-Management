/**
 * Hệ thống đồng hồ thời gian cho bàn chơi billiards
 */

// Biến lưu trữ interval cho đồng hồ
let timerInterval = null;

// Khởi tạo toàn bộ đồng hồ đếm thời gian
function initializeTimers() {
  console.log("Đang khởi tạo đồng hồ đếm thời gian...");

  // Xóa interval cũ nếu có
  if (timerInterval) {
    clearInterval(timerInterval);
  }

  // Cập nhật tất cả đồng hồ ngay lập tức
  updateAllTimers();

  // Thiết lập interval mới, cập nhật mỗi giây
  timerInterval = setInterval(updateAllTimers, 1000);
  console.log("Đã thiết lập interval cho đồng hồ:", timerInterval);
}

// Cập nhật tất cả đồng hồ đếm
function updateAllTimers() {
  const timerElements = document.querySelectorAll(".table-timer");
  // console.log("Đang cập nhật", timerElements.length, "đồng hồ");

  if (timerElements.length === 0) {
    console.warn("Không tìm thấy đồng hồ đếm nào trên trang");
  }

  timerElements.forEach((timer, index) => {
    updateSingleTimer(timer, index);
  });
}

// Cập nhật một đồng hồ cụ thể
function updateSingleTimer(timerElement, index) {
  try {
    // Lấy dữ liệu từ thuộc tính data
    const startTimeStr = timerElement.getAttribute("data-start-time");
    const pricePerHour = parseFloat(
      timerElement.getAttribute("data-price-per-hour") || 0
    );
    const tableId = timerElement.getAttribute("data-table-id");

    // Kiểm tra dữ liệu đầu vào
    if (!startTimeStr) {
      console.error(`Đồng hồ #${index}: Không có thời gian bắt đầu`);
      return;
    }

    // Chuyển đổi chuỗi thời gian thành đối tượng Date
    const startTime = new Date(startTimeStr);
    const now = new Date();

    // Kiểm tra tính hợp lệ của đối tượng Date
    if (isNaN(startTime.getTime())) {
      console.error(
        `Đồng hồ #${index}: Thời gian bắt đầu không hợp lệ:`,
        startTimeStr
      );
      return;
    }

    // Tính thời gian trôi qua (milliseconds)
    const elapsedMs = now - startTime;

    // Tính giờ, phút, giây từ milliseconds
    const elapsedSeconds = Math.floor(elapsedMs / 1000);
    const hours = Math.floor(elapsedSeconds / 3600);
    const minutes = Math.floor((elapsedSeconds % 3600) / 60);
    const seconds = elapsedSeconds % 60;

    // Định dạng chuỗi thời gian (HH:MM:SS)
    const timeString =
      String(hours).padStart(2, "0") +
      ":" +
      String(minutes).padStart(2, "0") +
      ":" +
      String(seconds).padStart(2, "0");

    // Cập nhật hiển thị thời gian
    timerElement.textContent = timeString;

    // Tính và cập nhật chi phí
    if (pricePerHour > 0) {
      // Tính chính xác thời gian sử dụng tính bằng giờ (với phần thập phân)
      // Ví dụ: 1 giờ 30 phút = 1.5 giờ
      const exactHours = elapsedMs / (1000 * 60 * 60);

      // Tính tiền = số giờ chính xác × giá mỗi giờ
      const cost = exactHours * pricePerHour;

      // Định dạng số tiền theo VND
      const costString = formatCurrency(cost);

      // Tìm và cập nhật phần tử hiển thị chi phí
      const costElement = document.querySelector(
        `.table-cost[data-table-id="${tableId}"]`
      );
      if (costElement) {
        costElement.textContent = costString;

        // Lưu giá trị chính xác vào data attribute để sử dụng khi cần
        costElement.setAttribute("data-exact-hours", exactHours.toFixed(5));
        costElement.setAttribute("data-exact-cost", cost.toFixed(2));
      } else {
        console.warn(
          `Đồng hồ #${index}: Không tìm thấy phần tử hiển thị chi phí cho bàn ${tableId}`
        );
      }
    }
  } catch (error) {
    console.error(`Đồng hồ #${index}: Lỗi khi cập nhật:`, error);
  }
}

// Định dạng số tiền theo định dạng VND
function formatCurrency(amount) {
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(amount);
}

// Khởi động hệ thống khi trang web được tải
document.addEventListener("DOMContentLoaded", function () {
  console.log("Trang đã tải xong, khởi động hệ thống đồng hồ");
  initializeTimers();
});
