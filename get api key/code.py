from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.chrome.service import Service
from webdriver_manager.chrome import ChromeDriverManager
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
import os

# The URL you want to access
URL = 'https://image-upscaling.net/account/en.html'

# The file to save the results
OUTPUT_FILE = 'client_ids.txt'

def run_scraper():
    """
    Opens the browser in incognito and headless mode,
    extracts the client ID, and saves it to a file.
    """
    chrome_options = Options()
    chrome_options.add_argument("--incognito")
    chrome_options.add_argument("--headless")

    driver = None
    try:
        # Initialize the Service with the driver path
        service = Service(ChromeDriverManager().install())

        # Initialize the WebDriver
        driver = webdriver.Chrome(service=service, options=chrome_options)
        
        # Open the URL
        driver.get(URL)

        # Wait up to 15 seconds for the element's text to change from "Loading..."
        wait = WebDriverWait(driver, 15)
        client_id_element = wait.until(
            EC.presence_of_element_located((By.ID, "client_id_display"))
        )
        wait.until(
            lambda d: client_id_element.text != "Loading..." and client_id_element.text != ""
        )

        # Extract the text from the element
        client_id_text = client_id_element.text

        # Check if the text is empty or None before proceeding
        if client_id_text:
            print(f"Trích xuất thành công client ID: {client_id_text}")
            
            # Write the client ID to the file
            with open(OUTPUT_FILE, 'a', encoding='utf-8') as f:
                f.write(client_id_text + '\n')
        else:
            print("Không thể trích xuất client ID. Giá trị là rỗng.")
            
    except Exception as e:
        print(f"Đã xảy ra lỗi: {e}")
        
    finally:
        if driver:
            driver.quit()

def main():
    """Repeats the scraping process for a user-specified number of times."""
    try:
        num_iterations_str = input("Nhập số lần lặp bạn muốn (mặc định 20): ")
        num_iterations = int(num_iterations_str) if num_iterations_str else 20
        print(f"Bắt đầu quá trình lặp lại {num_iterations} lần...")
    except ValueError:
        print("Đầu vào không hợp lệ. Vui lòng nhập một số nguyên.")
        return

    # Delete the old file if it exists
    if os.path.exists(OUTPUT_FILE):
        os.remove(OUTPUT_FILE)

    for i in range(1, num_iterations + 1):
        print(f"\n--- Lần lặp thứ {i} ---")
        run_scraper()
        
    print(f"\nQuá trình hoàn tất. Kết quả được lưu tại file '{OUTPUT_FILE}'.")

if __name__ == "__main__":
    main()