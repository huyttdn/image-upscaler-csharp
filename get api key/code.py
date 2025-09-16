from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.chrome.service import Service
from webdriver_manager.chrome import ChromeDriverManager
import time
import os

# Đường link bạn muốn truy cập
URL = 'https://image-upscaling.net/account/en.html'

# Tên file để lưu kết quả
OUTPUT_FILE = 'client_ids.txt'

def run_scraper():
    """Mở trình duyệt ở chế độ ẩn danh và chạy ngầm, trích xuất client ID và lưu vào file."""
    # Thiết lập tùy chọn cho trình duyệt Chrome
    chrome_options = Options()
    chrome_options.add_argument("--incognito")  # Chạy ẩn danh
    chrome_options.add_argument("--headless")   # Chạy ngầm

    try:
        # Khởi tạo Service với đường dẫn driver
        service = Service(ChromeDriverManager().install())

        # Khởi tạo WebDriver với Service và Options đã cấu hình
        driver = webdriver.Chrome(service=service, options=chrome_options)
        
        # Mở đường link
        driver.get(URL)

        # Đợi một chút để trang tải
        time.sleep(3)

        # Tìm kiếm thẻ span có id là "client_id_display" và trích xuất text
        client_id_element = driver.find_element(By.ID, "client_id_display")
        client_id_text = client_id_element.text

        print(f"Trích xuất thành công client ID: {client_id_text}")

        # Ghi client ID vào file
        with open(OUTPUT_FILE, 'a', encoding='utf-8') as f:
            f.write(client_id_text + '\n')
            
    except Exception as e:
        print(f"Đã xảy ra lỗi: {e}")
        
    finally:
        # Đảm bảo trình duyệt được đóng
        if 'driver' in locals():
            driver.quit()

def main():
    """Lặp lại quá trình với số lần do người dùng nhập."""
    try:
        num_iterations_str = input("Nhập số lần lặp bạn muốn (mặc định 20): ")
        if not num_iterations_str:
            num_iterations = 20  # Giá trị mặc định nếu người dùng không nhập gì
        else:
            num_iterations = int(num_iterations_str)
        print(f"Bắt đầu quá trình lặp lại {num_iterations} lần...")
    except ValueError:
        print("Đầu vào không hợp lệ. Vui lòng nhập một số nguyên.")
        return

    # Xóa file cũ nếu nó tồn tại để bắt đầu mới
    if os.path.exists(OUTPUT_FILE):
        os.remove(OUTPUT_FILE)

    for i in range(1, num_iterations + 1):
        print(f"\n--- Lần lặp thứ {i} ---")
        run_scraper()
        
    print(f"\nQuá trình hoàn tất. Kết quả được lưu tại file '{OUTPUT_FILE}'.")

if __name__ == "__main__":
    main()