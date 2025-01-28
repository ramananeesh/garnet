#!/bin/bash

# Run the experiment for BPTree
# Usage: ./run_exp.sh [num_entries] [num_trials] [output_file]

# Check the number of arguments
if [ "$#" -ne 2 ]; then
    echo "Usage: ./run_exp.sh [num_trials] [output_file]"
    exit 1
fi

# Number of entries -> 10 Million, 20 Million, 30 Million, 40 Million, 50 Million
NUM_ENTRIES=(10000000 20000000 30000000 40000000 50000000 60000000 70000000 80000000 90000000 100000000)
# NUM_ENTRIES=(1000000)
# Number of trials
NUM_TRIALS=$1
# Output file
OUTPUT_FILE=$2

# Compile the code
dotnet build -c release -f net8.0 BPTree.csproj
# loop through the number of entries
for i in "${NUM_ENTRIES[@]}"
do
    echo "Running experiment for $i entries"
    # Run the experiment
    for j in $(seq 1 $NUM_TRIALS)
    do
        # Run the experiment
        dotnet run bin/release/net8.0/BPTree -f net8.0 -N $i >> $OUTPUT_FILE
    done
done