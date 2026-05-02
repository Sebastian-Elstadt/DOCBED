from PIL import Image, ImageDraw
import io

def convert_page_to_image(file_bytes: bytes, filename: str):
    ext = filename.lower().split('.')[-1]
    
    if ext == "pdf":
        from pdf2image import convert_from_bytes, pdfinfo_from_bytes
        page_count = pdfinfo_from_bytes(file_bytes)["Pages"]
        for i in range(1, page_count + 1):
            pages = convert_from_bytes(file_bytes, dpi=200, first_page=i, last_page=i)
            yield pages[0]
    
    elif ext in ("png", "jpg", "jpeg", "tiff", "bmp"):
        yield Image.open(io.BytesIO(file_bytes))
    
    else:
        # Text files (txt, md, docx fallback)
        text = file_bytes.decode("utf-8", errors="ignore")
        img = Image.new("RGB", (1200, 1600), color=(255, 255, 255))
        draw = ImageDraw.Draw(img)
        draw.text((50, 50), text[:4000], fill=(0, 0, 0))
        yield img
