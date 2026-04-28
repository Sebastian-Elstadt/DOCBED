from pdf2image import convert_from_bytes
from PIL import Image, ImageDraw, ImageFont
import fitz  # PyMuPDF
from docx import Document
import io

def convert_to_images(file_bytes: bytes, filename: str):
    ext = filename.lower().split('.')[-1]
    
    if ext in ['pdf']:
        return convert_from_bytes(file_bytes, dpi=300)
    
    elif ext in ['png', 'jpg', 'jpeg', 'tiff', 'bmp']:
        return [Image.open(io.BytesIO(file_bytes))]
    
    elif ext in ['docx']:
        # Simple conversion - render text as image
        doc = Document(io.BytesIO(file_bytes))
        text = "\n\n".join([para.text for para in doc.paragraphs])
        img = Image.new('RGB', (1200, 1600), color=(255, 255, 255))
        draw = ImageDraw.Draw(img)
        draw.text((50, 50), text, fill=(0, 0, 0))
        return [img]
    
    elif ext in ['txt', 'md']:
        text = file_bytes.decode('utf-8')
        img = Image.new('RGB', (1200, 1600), color=(255, 255, 255))
        draw = ImageDraw.Draw(img)
        draw.text((50, 50), text, fill=(0, 0, 0))
        return [img]
    
    else:
        raise ValueError(f"Unsupported file type: {ext}")
