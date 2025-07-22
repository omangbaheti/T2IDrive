
def basic_eulers_circuit():
    els = list(range(6))
    sorted(els)

    adj = { el: sorted(els) for el in sorted(els)}

    print(*adj.items(), sep="\n")
    circuit = []
    current_path = []

    current_path = [next(iter(adj))]

    while len(current_path) >0:
        current_node = current_path[-1]

        if len(adj[current_node]) > 0:
            adjacent_nodes = adj[current_node]
            next_node = adjacent_nodes.pop()
            adj[current_node] = adjacent_nodes
            current_path.append(next_node)
        else:
            circuit.append(current_path.pop())


    transitions = []
    prev = None
    for i in range(len(circuit) - 1, -1, -1):
        print(circuit[i], end = "")
        if i:
            print(" -> ", end = "")

        if prev is not None:
            transitions.append((prev, circuit[i]))

        prev = circuit[i]

    print()
    print(sorted(transitions))


def two_step_eulers_circuit():
    els = list(range(6))
    sorted(els)

    adj = { el: [sorted(els), sorted(els)] for el in sorted(els)}

    circuit = []
    current_path = []

    current_path = [next(iter(adj))]

    use0 = False

    while len(current_path) >0:
        current_node = current_path[-1]

        if use0:
            if len(adj[current_node][0]) > 0:
                adjacent_nodes = adj[current_node][0]
                next_node = adjacent_nodes.pop()
                adj[current_node][0] = adjacent_nodes
                current_path.append(next_node)
                use0 = not use0
                print("0", current_node, adj[current_node])
            else:
                print("Adding")
                circuit.append(current_path.pop(-1))
        else:
            if len(adj[current_node][1]) > 0:
                adjacent_nodes = adj[current_node][1]
                next_node = adjacent_nodes.pop()
                adj[current_node][1] = adjacent_nodes
                current_path.append(next_node)
                use0 = not use0
                print("1", current_node, adj[current_node])
            else:
                circuit.append(current_path.pop(-1))


    type1pairs = []
    type2pairs = []
    prev = None
    for i in range(len(circuit) - 1, -1, -1):
        print(circuit[i], end = "")
        if use0:
            print(" -> ", end = "")
        else:
            print(" ==> ", end = "")

        if prev is not None:
            if use0:
                type1pairs.append((prev, circuit[i]))
            else:
                type2pairs.append((prev, circuit[i]))

        use0 = not use0
        prev = circuit[i]

    print()
    print(sorted(type1pairs), len(type1pairs))
    print(sorted(type2pairs), len(type2pairs))

if __name__ == "__main__":
    basic_eulers_circuit()
    two_step_eulers_circuit()