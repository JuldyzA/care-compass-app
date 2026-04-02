(function () {
    // Configuration from the Razor view
    const maxFileSizeBytes =
        window.userEditProfileConfig?.maxFileSizeBytes ?? 5 * 1024 * 1024;
    const uploadUrl = window.userEditProfileConfig?.uploadUrl;

    if (!uploadUrl) {
        console.error("Upload URL not configured for edit profile.");
        return;
    }

    const btnSelectProfilePhoto = document.getElementById(
        "btnSelectProfilePhoto",
    );
    const fileInput = document.getElementById("profilePhotoFileInput");
    const previewImg = document.getElementById("profilePhotoPreview");
    const previewInitials = document.getElementById(
        "profilePhotoPreviewInitials",
    );
    const avatarImg = document.getElementById("profileAvatarImg");
    const avatarInitials = document.getElementById("profileAvatarInitials");
    const hiddenUrlInput = document.getElementById("ProfilePhotoUrlHidden");
    const errorBlock = document.getElementById("profilePhotoUploadError");

    const modalBackdrop = document.getElementById("profilePhotoModalBackdrop");
    const modalImage = document.getElementById("profilePhotoModalImage");
    const cropArea = document.getElementById("profilePhotoCropArea");
    const cropInner = document.getElementById("profilePhotoCropInner");
    const btnCloseModal = document.getElementById("btnCloseProfilePhotoModal");
    const btnCancelModal = document.getElementById("btnCancelProfilePhoto");
    const btnSavePhoto = document.getElementById("btnSaveProfilePhoto");

    let selectedImageDataUrl = null;
    let isDragging = false;
    let dragStartX = 0;
    let dragStartY = 0;
    let imgStartLeft = 0;
    let imgStartTop = 0;
    let imgNaturalWidth = 0;
    let imgNaturalHeight = 0;

    function showError(message) {
        if (!errorBlock) return;
        errorBlock.textContent =
            message || "An error occurred while uploading the image.";
        errorBlock.style.display = "block";
    }

    function clearError() {
        if (!errorBlock) return;
        errorBlock.textContent = "";
        errorBlock.style.display = "none";
    }

    function openModal() {
        if (!modalBackdrop) return;
        modalBackdrop.style.display = "flex";
    }

    function closeModal() {
        if (!modalBackdrop) return;
        modalBackdrop.style.display = "none";
        selectedImageDataUrl = null;
        isDragging = false;
    }

    function clampPosition(left, top) {
        if (!cropArea || !modalImage) return { left, top };

        const cropRect = cropArea.getBoundingClientRect();
        const cropWidth = cropRect.width;
        const cropHeight = cropRect.height;

        const imgRect = modalImage.getBoundingClientRect();
        const imgWidth = imgRect.width;
        const imgHeight = imgRect.height;

        const minLeft = cropWidth - imgWidth;
        const minTop = cropHeight - imgHeight;

        return {
            left: Math.min(0, Math.max(minLeft, left)),
            top: Math.min(0, Math.max(minTop, top)),
        };
    }

    function centerImage() {
        if (!cropArea || !modalImage) return;

        const cropRect = cropArea.getBoundingClientRect();
        const cropWidth = cropRect.width;
        const cropHeight = cropRect.height;

        const imgAspect = imgNaturalWidth / imgNaturalHeight;
        const cropAspect = cropWidth / cropHeight;

        let drawWidth, drawHeight;
        if (imgAspect > cropAspect) {
            drawHeight = cropHeight;
            drawWidth = drawHeight * imgAspect;
        } else {
            drawWidth = cropWidth;
            drawHeight = drawWidth / imgAspect;
        }

        modalImage.style.width = drawWidth + "px";
        modalImage.style.height = drawHeight + "px";

        const left = (cropWidth - drawWidth) / 2;
        const top = (cropHeight - drawHeight) / 2;

        const clamped = clampPosition(left, top);
        modalImage.style.left = clamped.left + "px";
        modalImage.style.top = clamped.top + "px";
    }

    function onPointerDown(e) {
        if (!modalImage) return;

        isDragging = true;
        const evt = e.touches ? e.touches[0] : e;
        dragStartX = evt.clientX;
        dragStartY = evt.clientY;

        const style = window.getComputedStyle(modalImage);
        imgStartLeft = parseFloat(style.left || "0");
        imgStartTop = parseFloat(style.top || "0");

        document.addEventListener("mousemove", onPointerMove);
        document.addEventListener("mouseup", onPointerUp);
        document.addEventListener("touchmove", onPointerMove, {
            passive: false,
        });
        document.addEventListener("touchend", onPointerUp);
        e.preventDefault();
    }

    function onPointerMove(e) {
        if (!isDragging || !modalImage) return;

        const evt = e.touches ? e.touches[0] : e;
        const dx = evt.clientX - dragStartX;
        const dy = evt.clientY - dragStartY;

        let newLeft = imgStartLeft + dx;
        let newTop = imgStartTop + dy;

        const clamped = clampPosition(newLeft, newTop);
        modalImage.style.left = clamped.left + "px";
        modalImage.style.top = clamped.top + "px";

        e.preventDefault();
    }

    function onPointerUp(e) {
        isDragging = false;
        document.removeEventListener("mousemove", onPointerMove);
        document.removeEventListener("mouseup", onPointerUp);
        document.removeEventListener("touchmove", onPointerMove);
        document.removeEventListener("touchend", onPointerUp);
        e && e.preventDefault();
    }

    function getCroppedImageDataUrl() {
        if (!cropArea || !modalImage || !selectedImageDataUrl) {
            return selectedImageDataUrl;
        }

        const cropRect = cropArea.getBoundingClientRect();
        const cropWidth = cropRect.width;
        const cropHeight = cropRect.height;

        const style = window.getComputedStyle(modalImage);
        const imgDisplayWidth = parseFloat(style.width || "0");
        const imgDisplayHeight = parseFloat(style.height || "0");
        const imgLeft = parseFloat(style.left || "0");
        const imgTop = parseFloat(style.top || "0");

        if (
            !imgDisplayWidth ||
            !imgDisplayHeight ||
            !imgNaturalWidth ||
            !imgNaturalHeight
        ) {
            return selectedImageDataUrl;
        }

        const scaleX = imgNaturalWidth / imgDisplayWidth;
        const scaleY = imgNaturalHeight / imgDisplayHeight;

        const sx = -imgLeft * scaleX;
        const sy = -imgTop * scaleY;
        const sw = cropWidth * scaleX;
        const sh = cropHeight * scaleY;

        const canvas = document.createElement("canvas");
        canvas.width = cropWidth;
        canvas.height = cropHeight;
        const ctx = canvas.getContext("2d");

        const img = new Image();
        img.src = selectedImageDataUrl;

        return new Promise((resolve) => {
            img.onload = function () {
                ctx.drawImage(
                    img,
                    sx,
                    sy,
                    sw,
                    sh,
                    0,
                    0,
                    canvas.width,
                    canvas.height,
                );
                resolve(canvas.toDataURL("image/jpeg", 0.9));
            };
            img.onerror = function () {
                resolve(selectedImageDataUrl);
            };
        });
    }

    if (cropInner) {
        cropInner.addEventListener("mousedown", onPointerDown);
        cropInner.addEventListener("touchstart", onPointerDown, {
            passive: false,
        });
    }

    if (btnSelectProfilePhoto && fileInput) {
        btnSelectProfilePhoto.addEventListener("click", function () {
            clearError();
            fileInput.click();
        });

        fileInput.addEventListener("change", function (e) {
            clearError();
            const file = e.target.files && e.target.files[0];
            if (!file) {
                return;
            }

            if (!file.type.startsWith("image/")) {
                showError("Please select a valid image file.");
                fileInput.value = "";
                return;
            }

            if (file.size > maxFileSizeBytes) {
                showError("File size exceeds 5 MB limit.");
                fileInput.value = "";
                return;
            }

            const reader = new FileReader();
            reader.onload = function (loadEvent) {
                const dataUrl = loadEvent.target.result;
                selectedImageDataUrl = dataUrl;

                if (modalImage) {
                    modalImage.onload = function () {
                        imgNaturalWidth = modalImage.naturalWidth;
                        imgNaturalHeight = modalImage.naturalHeight;
                        centerImage();
                    };
                    modalImage.src = dataUrl;
                }
                openModal();
            };
            reader.onerror = function () {
                showError("Unable to read the selected file.");
            };

            reader.readAsDataURL(file);
        });
    }

    [btnCloseModal, btnCancelModal].forEach(function (btn) {
        if (btn) {
            btn.addEventListener("click", function () {
                closeModal();
                if (fileInput) {
                    fileInput.value = "";
                }
            });
        }
    });

    if (modalBackdrop) {
        modalBackdrop.addEventListener("click", function (e) {
            if (e.target === modalBackdrop) {
                closeModal();
                if (fileInput) {
                    fileInput.value = "";
                }
            }
        });
    }

    if (btnSavePhoto) {
        btnSavePhoto.addEventListener("click", async function () {
            clearError();

            if (!selectedImageDataUrl) {
                showError("No image selected.");
                return;
            }

            const finalDataUrl = await getCroppedImageDataUrl();

            const payload = {
                imageData: finalDataUrl,
                fileName: null,
            };

            fetch(uploadUrl, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    RequestVerificationToken:
                        document.querySelector(
                            'input[name="__RequestVerificationToken"]',
                        )?.value || "",
                },
                body: JSON.stringify(payload),
            })
                .then(async function (response) {
                    const data = await response.json().catch(() => ({}));
                    if (!response.ok || !data.success) {
                        const message =
                            data.message || "Failed to upload image.";
                        throw new Error(message);
                    }
                    return data;
                })
                .then(function (data) {
                    const newUrl = data.imageUrl;
                    if (!newUrl) {
                        throw new Error("Upload did not return an image URL.");
                    }

                    if (hiddenUrlInput) {
                        hiddenUrlInput.value = newUrl;
                    }

                    if (previewImg) {
                        previewImg.src = newUrl;
                        previewImg.style.display = "block";
                    } else {
                        const container = document.querySelector(
                            ".profile-photo-preview-container",
                        );
                        if (container) {
                            const imgEl = document.createElement("img");
                            imgEl.id = "profilePhotoPreview";
                            imgEl.alt = "Profile photo";
                            imgEl.src = newUrl;
                            container.innerHTML = "";
                            container.appendChild(imgEl);
                        }
                    }
                    if (previewInitials) {
                        previewInitials.style.display = "none";
                    }

                    if (avatarImg) {
                        avatarImg.src = newUrl;
                        avatarImg.style.display = "block";
                    }
                    if (avatarInitials) {
                        avatarInitials.style.display = "none";
                    }

                    closeModal();
                    if (fileInput) {
                        fileInput.value = "";
                    }
                })
                .catch(function (err) {
                    showError(
                        err.message ||
                        "An error occurred while uploading the image.",
                    );
                });
        });
    }
})();
