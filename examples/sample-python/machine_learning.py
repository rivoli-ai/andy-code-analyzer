#!/usr/bin/env python3
"""
Machine learning utilities and model implementations.
"""

import random
import math
from typing import List, Tuple, Dict, Optional, Callable
from dataclasses import dataclass
from abc import ABC, abstractmethod
import numpy as np


@dataclass
class DataPoint:
    """Represents a single data point."""
    features: List[float]
    label: Optional[float] = None
    
    def __len__(self):
        return len(self.features)


@dataclass
class Dataset:
    """Collection of data points."""
    data: List[DataPoint]
    
    def split(self, train_ratio: float = 0.8) -> Tuple['Dataset', 'Dataset']:
        """Split dataset into training and testing sets."""
        split_idx = int(len(self.data) * train_ratio)
        shuffled = self.data.copy()
        random.shuffle(shuffled)
        
        train_data = Dataset(shuffled[:split_idx])
        test_data = Dataset(shuffled[split_idx:])
        
        return train_data, test_data
    
    @property
    def size(self) -> int:
        return len(self.data)


class Model(ABC):
    """Abstract base class for machine learning models."""
    
    @abstractmethod
    def train(self, dataset: Dataset) -> None:
        """Train the model on a dataset."""
        pass
    
    @abstractmethod
    def predict(self, features: List[float]) -> float:
        """Make a prediction for given features."""
        pass
    
    def evaluate(self, dataset: Dataset) -> Dict[str, float]:
        """Evaluate model performance on a dataset."""
        if not dataset.data:
            return {"error": float('inf')}
        
        predictions = []
        actuals = []
        
        for point in dataset.data:
            if point.label is not None:
                pred = self.predict(point.features)
                predictions.append(pred)
                actuals.append(point.label)
        
        if not predictions:
            return {"error": float('inf')}
        
        # Calculate metrics
        mse = sum((p - a) ** 2 for p, a in zip(predictions, actuals)) / len(predictions)
        mae = sum(abs(p - a) for p, a in zip(predictions, actuals)) / len(predictions)
        
        return {
            "mse": mse,
            "mae": mae,
            "rmse": math.sqrt(mse)
        }


class LinearRegression(Model):
    """Simple linear regression implementation."""
    
    def __init__(self, learning_rate: float = 0.01, epochs: int = 1000):
        self.learning_rate = learning_rate
        self.epochs = epochs
        self.weights: Optional[List[float]] = None
        self.bias: float = 0.0
    
    def train(self, dataset: Dataset) -> None:
        """Train using gradient descent."""
        if not dataset.data:
            return
        
        # Initialize weights
        n_features = len(dataset.data[0].features)
        self.weights = [0.0] * n_features
        self.bias = 0.0
        
        # Gradient descent
        for epoch in range(self.epochs):
            total_error = 0.0
            
            for point in dataset.data:
                if point.label is None:
                    continue
                
                # Forward pass
                prediction = self._forward(point.features)
                error = prediction - point.label
                total_error += error ** 2
                
                # Backward pass
                for i in range(n_features):
                    self.weights[i] -= self.learning_rate * error * point.features[i]
                self.bias -= self.learning_rate * error
            
            if epoch % 100 == 0:
                avg_error = total_error / dataset.size
                print(f"Epoch {epoch}, Average Error: {avg_error:.4f}")
    
    def predict(self, features: List[float]) -> float:
        """Make a prediction."""
        if self.weights is None:
            raise ValueError("Model not trained yet")
        
        return self._forward(features)
    
    def _forward(self, features: List[float]) -> float:
        """Forward pass computation."""
        result = self.bias
        for i, feature in enumerate(features):
            result += self.weights[i] * feature
        return result


class KNearestNeighbors(Model):
    """K-Nearest Neighbors classifier/regressor."""
    
    def __init__(self, k: int = 3, distance_metric: str = "euclidean"):
        self.k = k
        self.distance_metric = distance_metric
        self.training_data: Optional[Dataset] = None
    
    def train(self, dataset: Dataset) -> None:
        """KNN doesn't need training, just stores the data."""
        self.training_data = dataset
    
    def predict(self, features: List[float]) -> float:
        """Predict using k nearest neighbors."""
        if self.training_data is None:
            raise ValueError("Model not trained yet")
        
        # Calculate distances to all training points
        distances = []
        for point in self.training_data.data:
            if point.label is not None:
                dist = self._calculate_distance(features, point.features)
                distances.append((dist, point.label))
        
        # Sort by distance and take k nearest
        distances.sort(key=lambda x: x[0])
        k_nearest = distances[:self.k]
        
        # Return average of k nearest labels (for regression)
        if k_nearest:
            return sum(label for _, label in k_nearest) / len(k_nearest)
        return 0.0
    
    def _calculate_distance(self, a: List[float], b: List[float]) -> float:
        """Calculate distance between two points."""
        if self.distance_metric == "euclidean":
            return math.sqrt(sum((x - y) ** 2 for x, y in zip(a, b)))
        elif self.distance_metric == "manhattan":
            return sum(abs(x - y) for x, y in zip(a, b))
        else:
            raise ValueError(f"Unknown distance metric: {self.distance_metric}")


class DecisionTree(Model):
    """Simple decision tree implementation."""
    
    def __init__(self, max_depth: int = 5, min_samples_split: int = 2):
        self.max_depth = max_depth
        self.min_samples_split = min_samples_split
        self.root = None
    
    class Node:
        """Decision tree node."""
        def __init__(self):
            self.feature_idx: Optional[int] = None
            self.threshold: Optional[float] = None
            self.left: Optional['DecisionTree.Node'] = None
            self.right: Optional['DecisionTree.Node'] = None
            self.value: Optional[float] = None
            self.is_leaf: bool = False
    
    def train(self, dataset: Dataset) -> None:
        """Build the decision tree."""
        self.root = self._build_tree(dataset.data, 0)
    
    def predict(self, features: List[float]) -> float:
        """Traverse the tree to make a prediction."""
        if self.root is None:
            raise ValueError("Model not trained yet")
        
        return self._traverse_tree(features, self.root)
    
    def _build_tree(self, data: List[DataPoint], depth: int) -> Node:
        """Recursively build the decision tree."""
        node = self.Node()
        
        # Check stopping criteria
        if (depth >= self.max_depth or 
            len(data) < self.min_samples_split or
            not data):
            node.is_leaf = True
            node.value = self._calculate_leaf_value(data)
            return node
        
        # Find best split
        best_feature, best_threshold = self._find_best_split(data)
        
        if best_feature is None:
            node.is_leaf = True
            node.value = self._calculate_leaf_value(data)
            return node
        
        # Split data
        left_data, right_data = self._split_data(data, best_feature, best_threshold)
        
        # Build subtrees
        node.feature_idx = best_feature
        node.threshold = best_threshold
        node.left = self._build_tree(left_data, depth + 1)
        node.right = self._build_tree(right_data, depth + 1)
        
        return node
    
    def _find_best_split(self, data: List[DataPoint]) -> Tuple[Optional[int], Optional[float]]:
        """Find the best feature and threshold to split on."""
        if not data:
            return None, None
        
        best_feature = None
        best_threshold = None
        best_variance_reduction = 0
        
        n_features = len(data[0].features)
        
        for feature_idx in range(n_features):
            # Get unique values for this feature
            values = sorted(set(point.features[feature_idx] for point in data))
            
            for i in range(len(values) - 1):
                threshold = (values[i] + values[i + 1]) / 2
                
                # Calculate variance reduction
                left_data, right_data = self._split_data(data, feature_idx, threshold)
                
                if left_data and right_data:
                    var_reduction = self._calculate_variance_reduction(
                        data, left_data, right_data
                    )
                    
                    if var_reduction > best_variance_reduction:
                        best_variance_reduction = var_reduction
                        best_feature = feature_idx
                        best_threshold = threshold
        
        return best_feature, best_threshold
    
    def _split_data(self, data: List[DataPoint], feature_idx: int, 
                    threshold: float) -> Tuple[List[DataPoint], List[DataPoint]]:
        """Split data based on feature and threshold."""
        left = [p for p in data if p.features[feature_idx] <= threshold]
        right = [p for p in data if p.features[feature_idx] > threshold]
        return left, right
    
    def _calculate_variance_reduction(self, parent: List[DataPoint], 
                                    left: List[DataPoint], 
                                    right: List[DataPoint]) -> float:
        """Calculate variance reduction from a split."""
        def variance(data: List[DataPoint]) -> float:
            if not data:
                return 0
            labels = [p.label for p in data if p.label is not None]
            if not labels:
                return 0
            mean = sum(labels) / len(labels)
            return sum((l - mean) ** 2 for l in labels) / len(labels)
        
        parent_var = variance(parent)
        left_var = variance(left)
        right_var = variance(right)
        
        n_parent = len(parent)
        n_left = len(left)
        n_right = len(right)
        
        weighted_var = (n_left / n_parent) * left_var + (n_right / n_parent) * right_var
        
        return parent_var - weighted_var
    
    def _calculate_leaf_value(self, data: List[DataPoint]) -> float:
        """Calculate the value for a leaf node."""
        labels = [p.label for p in data if p.label is not None]
        return sum(labels) / len(labels) if labels else 0.0
    
    def _traverse_tree(self, features: List[float], node: Node) -> float:
        """Traverse the tree to make a prediction."""
        if node.is_leaf:
            return node.value
        
        if features[node.feature_idx] <= node.threshold:
            return self._traverse_tree(features, node.left)
        else:
            return self._traverse_tree(features, node.right)


class NeuralNetwork(Model):
    """Simple feed-forward neural network."""
    
    def __init__(self, layer_sizes: List[int], learning_rate: float = 0.01, 
                 epochs: int = 1000):
        self.layer_sizes = layer_sizes
        self.learning_rate = learning_rate
        self.epochs = epochs
        self.weights: List[List[List[float]]] = []
        self.biases: List[List[float]] = []
        
        # Initialize weights and biases
        for i in range(len(layer_sizes) - 1):
            weight_matrix = [[random.gauss(0, 0.1) 
                             for _ in range(layer_sizes[i])] 
                            for _ in range(layer_sizes[i + 1])]
            bias_vector = [0.0 for _ in range(layer_sizes[i + 1])]
            
            self.weights.append(weight_matrix)
            self.biases.append(bias_vector)
    
    def train(self, dataset: Dataset) -> None:
        """Train the neural network using backpropagation."""
        for epoch in range(self.epochs):
            total_error = 0.0
            
            for point in dataset.data:
                if point.label is None:
                    continue
                
                # Forward pass
                activations = self._forward_pass(point.features)
                prediction = activations[-1][0]  # Assuming single output
                error = prediction - point.label
                total_error += error ** 2
                
                # Backward pass
                self._backward_pass(point.features, point.label, activations)
            
            if epoch % 100 == 0:
                avg_error = total_error / dataset.size
                print(f"Epoch {epoch}, Average Error: {avg_error:.4f}")
    
    def predict(self, features: List[float]) -> float:
        """Make a prediction using the neural network."""
        activations = self._forward_pass(features)
        return activations[-1][0]  # Assuming single output
    
    def _forward_pass(self, inputs: List[float]) -> List[List[float]]:
        """Perform forward pass through the network."""
        activations = [inputs]
        
        for i in range(len(self.weights)):
            layer_output = []
            
            for j in range(len(self.weights[i])):
                # Calculate weighted sum
                total = self.biases[i][j]
                for k in range(len(activations[-1])):
                    total += self.weights[i][j][k] * activations[-1][k]
                
                # Apply activation function (ReLU)
                layer_output.append(max(0, total))
            
            activations.append(layer_output)
        
        return activations
    
    def _backward_pass(self, inputs: List[float], target: float, 
                      activations: List[List[float]]) -> None:
        """Perform backward pass to update weights."""
        # Simplified backpropagation for demonstration
        # In practice, you would compute gradients properly
        
        output_error = activations[-1][0] - target
        
        # Update output layer weights
        for i in range(len(self.weights[-1][0])):
            self.weights[-1][0][i] -= self.learning_rate * output_error * activations[-2][i]
        self.biases[-1][0] -= self.learning_rate * output_error


def generate_synthetic_data(n_samples: int = 100, n_features: int = 2, 
                          noise: float = 0.1) -> Dataset:
    """Generate synthetic dataset for testing."""
    data = []
    
    for _ in range(n_samples):
        features = [random.uniform(-10, 10) for _ in range(n_features)]
        # Simple linear relationship with noise
        label = sum(f * (i + 1) for i, f in enumerate(features)) + random.gauss(0, noise)
        data.append(DataPoint(features, label))
    
    return Dataset(data)


def main():
    """Example usage of machine learning models."""
    # Generate synthetic data
    dataset = generate_synthetic_data(n_samples=200, n_features=3)
    train_data, test_data = dataset.split(train_ratio=0.8)
    
    print("=== Linear Regression ===")
    lr_model = LinearRegression(learning_rate=0.001, epochs=500)
    lr_model.train(train_data)
    lr_metrics = lr_model.evaluate(test_data)
    print(f"Test metrics: {lr_metrics}")
    
    print("\n=== K-Nearest Neighbors ===")
    knn_model = KNearestNeighbors(k=5)
    knn_model.train(train_data)
    knn_metrics = knn_model.evaluate(test_data)
    print(f"Test metrics: {knn_metrics}")
    
    print("\n=== Decision Tree ===")
    dt_model = DecisionTree(max_depth=5)
    dt_model.train(train_data)
    dt_metrics = dt_model.evaluate(test_data)
    print(f"Test metrics: {dt_metrics}")


if __name__ == "__main__":
    main()