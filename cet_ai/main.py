import json
import socket

HOST = "localhost"
PORT = 5000


def run_server():
    while True:
        try:
            server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            server_socket.bind((HOST, PORT))
            server_socket.listen(1)
            print(f"[Server] Aguardando conexão em {HOST}:{PORT}...")

            conn, addr = server_socket.accept()
            print(f"[Server] Conectado por {addr}")

            buffer = ""

            try:
                while True:
                    data = conn.recv(4096)
                    if not data:
                        break

                    buffer += data.decode("utf-8")

                    while "\n" in buffer:
                        line, buffer = buffer.split("\n", 1)
                        if line.strip() == "":
                            continue
                        try:
                            game_state = json.loads(line)
                            handle_game_state(game_state)
                            conn.sendall(b"ack\n")
                        except json.JSONDecodeError as e:
                            print(f"[Server] Erro de JSON: {e}")
            except Exception as e:
                print(f"[Server] Erro geral: {e}")
            finally:
                conn.close()
                server_socket.close()
                print("[Server] Conexão encerrada.")
        except KeyboardInterrupt:
            print("[Server] Encerrando servidor...")
            break
        except Exception as e:
            print(f"[Server] Erro ao abrir o servidor: {e}")


def handle_game_state(game_state):
    print(f"[GameState recebido] {game_state}")


if __name__ == "__main__":
    run_server()
