from fastapi import FastAPI, UploadFile, File
from fastapi.responses import JSONResponse
from converter import convert_to_images
import base64
import io

app = FastAPI(title="DOCBED_DOC_CONVERTER")

@app.post("/convert")
async def convert_document(file: UploadFile = File(...)):
    try:
        content = await file.read()
        images = convert_to_images(content, file.filename)
        
        # Return list of base64 encoded JPEGs
        result = []
        for i, img in enumerate(images):
            buffer = io.BytesIO()
            img.save(buffer, format="JPEG", quality=95)
            img_base64 = base64.b64encode(buffer.getvalue()).decode('utf-8')
            result.append({
                "page": i + 1,
                "image_base64": img_base64,
                "format": "jpeg"
            })
            
        return JSONResponse(content={"success": True, "pages": result})
    
    except Exception as e:
        return JSONResponse(
            status_code=500,
            content={"success": False, "error": str(e)}
        )
