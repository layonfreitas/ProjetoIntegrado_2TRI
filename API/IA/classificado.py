import sys
import os
import csv
import json

from sklearn.tree import DecisionTreeClassifier


DATASET_PATH = os.path.join(os.path.dirname(__file__), "dataset.csv")


def load_dataset(path):
    X, y = [], []

    with open(path, newline="", encoding="utf-8") as f:
        reader = csv.reader(f)
        next(reader, None)  # Ignora o cabeçalho

        for row in reader:
            *features, label = row
            X.append([float(value) for value in features])
            y.append(label)

    return X, y


def main():
    if len(sys.argv) < 2:
        print(json.dumps({"error": "missing sample argument"}))
        sys.exit(1)

    try:
        sample = json.loads(sys.argv[1])
    except json.JSONDecodeError:
        print(json.dumps({
            "error": "sample argument is not valid JSON"
        }))
        sys.exit(1)

    if (
        not isinstance(sample, list)
        or not all(
            isinstance(value, (int, float)) and not isinstance(value, bool)
            for value in sample
        )
    ):
        print(json.dumps({
            "error": "sample must be a JSON array of numbers"
        }))
        sys.exit(1)

    try:
        X, y = load_dataset(DATASET_PATH)
    except FileNotFoundError:
        print(json.dumps({
            "error": "dataset.csv not found"
        }))
        sys.exit(1)

    if not X:
        print(json.dumps({
            "error": "dataset is empty"
        }))
        sys.exit(1)

    expected_features = len(X[0])

    if len(sample) != expected_features:
        print(json.dumps({
            "error": (
                f"sample must have {expected_features} features, "
                f"got {len(sample)}"
            )
        }))
        sys.exit(1)

    # Cria e treina o modelo de Árvore de Decisão
    decision_tree = DecisionTreeClassifier(random_state=42)
    decision_tree.fit(X, y)

    # Faz a previsão
    prediction = decision_tree.predict([sample])[0]

    # Calcula as probabilidades
    proba = decision_tree.predict_proba([sample])[0]

    probabilities = {
        label: float(probability)
        for label, probability in zip(
            decision_tree.classes_,
            proba
        )
    }

    print(json.dumps({
        "classification": prediction,
        "probabilities": probabilities
    }))


if __name__ == "__main__":
    main()