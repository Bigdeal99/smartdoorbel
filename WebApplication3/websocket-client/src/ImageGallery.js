import React, { useEffect, useState } from 'react';
import './ImageGallery.css';

const ImageGallery = ({ refresh }) => {
    const [images, setImages] = useState([]);

    const fetchImages = async () => {
        try {
            const response = await fetch('http://localhost:5164/api/images');
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            const data = await response.json();
            setImages(data);
        } catch (error) {
            console.error('Error fetching images:', error);
        }
    };

    useEffect(() => {
        fetchImages();
    }, [refresh]);

    const deleteImage = async (url) => {
        const fileName = url.split('/').pop();
        try {
            const response = await fetch(`http://localhost:5164/api/images/${fileName}`, {
                method: 'DELETE',
            });

            if (response.ok) {
                setImages(images.filter((image) => image.url !== url));
            } else {
                console.error('Failed to delete image');
            }
        } catch (error) {
            console.error('Error deleting image:', error);
        }
    };

    return (
        <div className="image-gallery">
            {images.map((image, index) => (
                <div key={index} className="image-container">
                    <img src={image.url} alt={`Stream ${index}`} />
                    <p>{image.fileName}</p>
                    <button onClick={() => deleteImage(image.url)}>Delete</button>
                </div>
            ))}
        </div>
    );
};

export default ImageGallery;
