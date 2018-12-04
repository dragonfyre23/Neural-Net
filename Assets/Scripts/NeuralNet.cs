﻿using System;
using System.Collections;
using System.Collections.Generic;

public class NeuralNet:IGeneticIndividual
{
    int numInputs;          //number of nodes in the input layer of the net
    int numOutputs;         //number of nodes in the output layer of the net
    int numHiddenLayers;    //the number of hidden layers
    int hiddenLayerSize;    //the number of nodes in each hidden layer //DOES NOT SUPPORT VARYING HIDDEN LAYER SIZES// although I don't see why not(8/29)
    double learningRate;       //factor to multiply deltaweights by during backpropagation
    Layer[] layers; //including hidden layers and output layers but not input layer. Logic works by attaching a set of weights to before a layer.
    public double numTestSets;
    public double[,] TestInputsSets;//first column is index and the second column is the test set
    public double[,] TestOutputSets;

    public Layer[] Layers
    {
        get
        {
            return layers;
        }
    }

    public int NumInputs
    {
        get
        {
            return numInputs;
        }

        set
        {
            numInputs = value;
        }
    }

    public int NumOutputs
    {
        get
        {
            return numOutputs;
        }
    }

    public int NumHiddenLayers
    {
        get
        {
            return numHiddenLayers;
        }
    }

    public int HiddenLayerSize
    {
        get
        {
            return hiddenLayerSize;
        }
    }

    public double LearningRate
    {
        get
        {
            return learningRate;
        }

        set
        {
            learningRate = value;
        }
    }

    public NeuralNet(int numInputs, int numOutputs, int numHiddenLayers, int hiddenLayerSize, double learningRate)
    {
        this.numInputs = numInputs;
        this.numOutputs = numOutputs;
        this.numHiddenLayers = numHiddenLayers;
        this.hiddenLayerSize = hiddenLayerSize;
        this.learningRate = learningRate;

        //instantiate layers
        layers = new Layer[numHiddenLayers + 1];    //+1 for output layer
        //assign first term (different because must fit into the input layer)
        layers[0] = new Layer(numInputs, hiddenLayerSize);
        for (int i = 1; i < layers.Length - 1; i++)
        {
            layers[i] = new Layer(hiddenLayerSize, hiddenLayerSize);
        }
        //assign last term (output Layer)
        layers[layers.Length - 1] = new Layer(hiddenLayerSize, numOutputs);
        
    }

    public double[] FeedForward(double[] inputValues) //inputValues.Length must equal numInputs //will return double[] with length = numOutputs
    {
        double[] lastValues = new double[hiddenLayerSize]; //holds values of the last layer to be passed into the current layer
        lastValues = layers[0].FeedForward(inputValues);
        for (int i = 1; i < layers.Length - 1; i++)
        {
            lastValues = layers[i].FeedForward(lastValues);//if you get an error on this line check the outputs and target array sizes. Maybe net constructed wrong?
        }
        return layers[layers.Length - 1].FeedForward(lastValues);
    }

    public NeuralNet Backpropagate(double[] inputValues, double[] targets)//targets must have length equal to numOutputs. usage is net = net.Backpropagate();
    {
        this.FeedForward(inputValues);
        Layer[] updatedLayers = new Layer[numHiddenLayers + 1];
        //output layer
        double[,] deltaWeights = layers[layers.Length - 1].BackpropagateOutput(targets);
        double[,] updatedWeights = new double[numOutputs, hiddenLayerSize + 1];
        for (int i = 0; i < numOutputs; i++)
        {
            for (int j = 0; j < hiddenLayerSize + 1; j++)
            {
                updatedWeights[i, j] = layers[layers.Length - 1].Weights[i, j] + learningRate*deltaWeights[i, j];
                //updatedWeights[i, j] = Functions.Sigmoid(updatedWeights[i, j]);
            }
        }
        updatedLayers[updatedLayers.Length - 1] = new Layer(hiddenLayerSize, numOutputs, updatedWeights);

        updatedWeights = new double[hiddenLayerSize, hiddenLayerSize + 1];      //hidden layers
        for (int hiddenLayerIndex = numHiddenLayers - 1; hiddenLayerIndex > 0; hiddenLayerIndex--)
        {
            deltaWeights = layers[hiddenLayerIndex].BackpropagateHidden(deltaWeights, layers[hiddenLayerIndex + 1].Weights);
            for (int i = 0; i < hiddenLayerSize; i++)
            {
                for (int j = 0; j < hiddenLayerSize; j++)
                {
                    updatedWeights[i, j] = layers[hiddenLayerIndex].Weights[i, j] + learningRate*deltaWeights[i, j];
                    //updatedWeights[i, j] = Functions.Sigmoid(updatedWeights[i, j]);
                }
            }
            updatedLayers[hiddenLayerIndex] = new Layer(hiddenLayerSize, hiddenLayerSize, updatedWeights);
        }

        updatedWeights = new double[hiddenLayerSize, numInputs + 1];            //first hidden layer
        deltaWeights = layers[0].BackpropagateHidden(deltaWeights, layers[1].Weights);
        for (int i = 0; i < hiddenLayerSize; i++)
        {
            for (int j = 0; j < numInputs; j++)
            {
                updatedWeights[i, j] = layers[0].Weights[i, j] + learningRate*deltaWeights[i, j];
                //updatedWeights[i, j] = Functions.Sigmoid(updatedWeights[i, j]);
            }
        }
        updatedLayers[0] = new Layer(numInputs, hiddenLayerSize, updatedWeights);

        NeuralNet updatedNet = new NeuralNet(numInputs, numOutputs, numHiddenLayers, hiddenLayerSize, learningRate);
        updatedNet.layers = updatedLayers;
        return updatedNet;
    }

    public double Fitness()//return the average fitness when all test sets are fed through the network. Will return a value between 0 and numOutputs. Low fitness is bad, high fitness is good
    {
        double errorTotal = 0;
        for (int i = 0; i < numTestSets; i++)//iterate for each test set
        {
            double[] TestInputSet = new double[numInputs];//create a temp variable for the input set
            for (int j = 0; j < numInputs; j++)//fill temp array
            {
                TestInputSet[j] = TestInputsSets[i, j];
            }

            double[] TestOutputSet = new double[numOutputs];//create a temp variable for the output set
            for (int j = 0; j < numOutputs; j++)//fill temp array
            {
                TestOutputSet[j] = TestOutputSets[i, j];
            }

            double[] TestResult = FeedForward(TestInputSet);//feed forward

            for (int j = 0; j < numOutputs; j++)//compare result to expected result and add to 
            {
                double error = Math.Abs(TestOutputSet[j] - TestResult[j]);//abs to keep error positive
                errorTotal += error;
            }
        }
        double fitness = numOutputs-(errorTotal / numTestSets);//take the average value. should be between 0 and numOutputs
        return fitness;
    }

    public void Randomize()
    {
        //randomize layers
        layers = new Layer[numHiddenLayers + 1];    //+1 for output layer
        //assign first term (different because must fit into the input layer)
        layers[0] = new Layer(numInputs, hiddenLayerSize);
        for (int i = 1; i < layers.Length - 1; i++)
        {
            layers[i] = new Layer(hiddenLayerSize, hiddenLayerSize);
        }
        //assign last term (output Layer)
        layers[layers.Length - 1] = new Layer(hiddenLayerSize, numOutputs);
    }

    public IGeneticIndividual[] Reproduce(IGeneticIndividual[] parents, int crossoverPoints, int numChildren)
    {
        throw new NotImplementedException();//todo
    }

    public void Mutate()//set a single random weight to a random value from 0 to 1
    {
        //layer to mutate//
        int i = (int)(RandHolder.NextDouble()*(numHiddenLayers + 1));//+1 for output
        //node to mutate//
        int j = -1;//should cause an index out of bounds if not set by subsequent if cases
        if (i == 0) {//if first hidden layer (same as else case)(same number of nodes as hidden layer so doesn't matter)
            j = (int)(RandHolder.NextDouble() * hiddenLayerSize);
        }
        else if (i == numHiddenLayers) {//if output layer
            j = (int)(RandHolder.NextDouble() * numOutputs);
        }
        else {//if any other hidden layer
            j = (int)(RandHolder.NextDouble() * hiddenLayerSize);
        }
        //weight to mutate//
        int k = -1;//should cause an index out of bounds if not set by subsequent if cases
        if (i == 0)//if first hidden layer (same as else case)(same number of nodes as hidden layer so doesn't matter)
        {
            k = (int)(RandHolder.NextDouble() * numInputs);
        }
        else if (i == numHiddenLayers)//if output layer same as else case
        {
            k = (int)(RandHolder.NextDouble() * hiddenLayerSize);
        }
        else//if any other hidden layer
        {
            k = (int)(RandHolder.NextDouble() * hiddenLayerSize);
        }
        //set new value;
        double newValue = RandHolder.NextDouble();
        Layers[i].Weights[j, k] = newValue;
    }

    public int CompareTo(IGeneticIndividual obj)//to be used by Arrays.sort in GeneticAlgorithm.cs>FitnessSort()
    {
        if(this.Fitness()>obj.Fitness())
        {
            return -1;//precede in sort order
        }
        else if(this.Fitness() < obj.Fitness())
        {
            return 1;//succeed in sort order
        }
        else
        {
            return 0;//equal in sort
        }
    }

    public int CompareTo(object obj)//should never be called because should never be compared to other data types
    {
        throw new NotImplementedException();
    }
}