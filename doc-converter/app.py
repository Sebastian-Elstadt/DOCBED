from fastapi import FastAPI, UploadFile, File
from fastapi.responses import StreamingResponse
from converter import convert_page_to_image
import json
import base64
import io
import logging

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(title="Document Processor")


def generate_pages(file_bytes: bytes, filename: str):
    logger.info(f"Processing file: {filename} ({len(file_bytes)} bytes)")
    
    page_number = 0
    generated = 0
    
    try:
        for img in convert_page_to_image(file_bytes, filename):
            page_number += 1
            if img is None:
                logger.error(f"Page {page_number}: Image is None!")
                continue
                
            buffer = io.BytesIO()
            img.save(buffer, format="JPEG", quality=85, optimize=True)
            img_base64 = base64.b64encode(buffer.getvalue()).decode("utf-8")
            
            logger.info(f"Page {page_number}: Generated {len(img_base64)} char base64")
            
            yield json.dumps({
                "success": True,
                "page": page_number,
                "image_base64": img_base64,
                "format": "jpeg",
                "debug_size": len(img_base64)
            }) + "\n"
            generated += 1
            
        if generated == 0:
            logger.error("No pages were generated!")
            yield json.dumps({
                "success": False,
                "error": "No pages generated from document",
                "debug": "Check converter logs - possibly missing poppler or unsupported format"
            }) + "\n"
            
    except Exception as e:
        logger.exception("Error in generate_pages")
        yield json.dumps({
            "success": False,
            "error": str(e)
        }) + "\n"


@app.post("/convert")
async def convert_document(file: UploadFile = File(...)):
    content = await file.read()
    logger.info(f"Received upload: {file.filename} ({len(content)} bytes)")
    
    return StreamingResponse(
        generate_pages(content, file.filename),
        media_type="application/x-ndjson"
    )
