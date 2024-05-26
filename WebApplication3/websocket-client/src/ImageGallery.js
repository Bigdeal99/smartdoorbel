import React, { useEffect, useState } from 'react';
import './ImageGallery.css';

const ImageGallery = () => {
    const [images, setImages] = useState([]);

    useEffect(() => {
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

        fetchImages();
    }, []);

    const deleteImage = async (fileName) => {
        try {
            const response = await fetch(`http://localhost:5164/api/images/${fileName}`, {
                method: 'DELETE',
            });

            if (response.ok) {
                setImages(images.filter((image) => image.Name !== fileName));
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
                    <img src={image.Url} alt={`Stream ${index}`} />
                    <p>{image.Name}</p>
                    <button onClick={() => deleteImage(image.Name)}>Delete</button>
                </div>
            ))}
        </div>
    );
};

export default ImageGallery;
