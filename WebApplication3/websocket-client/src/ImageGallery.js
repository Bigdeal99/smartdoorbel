import React, { useEffect, useState } from 'react';
import './ImageGallery.css';

const ImageGallery = () => {
    const [images, setImages] = useState([]);

    useEffect(() => {
        const fetchImages = async () => {
            try {
                const response = await fetch('http://localhost:5164/api/images'); // Updated port
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                const data = await response.json();
                setImages(data);
            } catch (error) {
                console.error('Error fetching images:', error);
            }
        };

        fetchImages();
    }, []);

    return (
        <div className="image-gallery">
            {images.map((url, index) => (
                <div key={index} className="image-container">
                    <img src={url} alt={`Stream ${index}`} />
                </div>
            ))}
        </div>
    );
};

export default ImageGallery;
