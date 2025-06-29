const alertsList = document.getElementById("alerts") as HTMLOListElement;

export function createAlert(message: string, timeout: number | null = 5000): () => void {
    const element = document.createElement("li") as HTMLLIElement;
    element.innerText = message;

    if (timeout != null) {
        const progress = document.createElement("span");
        progress.classList.add("progress");
        element.appendChild(progress);
        const animation = progress.animate(
            [
                { right: "0%" },
                { right: "100%" }
            ],
            {
                duration: timeout,
                fill: "forwards"
            }
        );
        animation.addEventListener("finish", () => {
            element.remove();
        });
        element.addEventListener("mouseenter", () => {
            animation.pause();
        });
        element.addEventListener("mouseleave", () => {
            animation.play();
        });
    }

    element.addEventListener("click", () => element.remove());

    alertsList.append(element);

    return () => element.remove();
}
