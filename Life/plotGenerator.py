import matplotlib.pyplot as plt
import os

file_dir = os.path.dirname(os.path.abspath(__file__))
data_path = os.path.join(file_dir, 'data.txt')

# Чтение данных из файла
with open(data_path, 'r') as file:
    data = [line.split() for line in file if line.strip()]
    
density = [float(x[0].replace(',', '.')) for x in data]
generation = [int(x[1]) for x in data]

output_path = os.path.join(file_dir, 'plot.png')

plt.plot(density, generation, 'ro-')
plt.title('The plot of transition to a Stable State')
plt.xlabel('Density')
plt.ylabel('Number of generation')
plt.grid(True)
plt.savefig('plot.png', dpi=300)
plt.show()