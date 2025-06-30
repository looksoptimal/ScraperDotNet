from ollama import Client
import argparse

def ask_ollama_vision_model(model_name: str, prompt: str, image_path: str):
    """
    Function to interact with the Ollama model.

    Parameters:
        model_name (str): Name of the model to use.
        prompt (str): Prompt (question) to send to the model.
        image_path (str): Path to the image file.

    Returns:
        response from the model
    """
    client = Client()
    response = client.generate(
        model=model_name,
        prompt=prompt,
        images=[image_path]
    )

    return response['response']

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='Ask Ollama vision model about an image')
    parser.add_argument('--model', type=str, required=True, help='Name of the model to use')
    parser.add_argument('--image', type=str, required=True, help='Path to the image file')
    parser.add_argument('--prompt', type=str, required=True, help='Prompt/question for the model')
    
    args = parser.parse_args()
    
    response = ask_ollama_vision_model(
        model_name=args.model,
        prompt=args.prompt,
        image_path=args.image
    )
    print(response)