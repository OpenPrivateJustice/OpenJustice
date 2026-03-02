#!/bin/bash
# Inicia ambos os projetos AtrocidadesRSS (Reader + Generator)

echo "Iniciando AtrocidadesRSS Reader (porta 5280)..."
cd /home/eduardo/Projects/AtrocidadesRSS/src/AtrocidadesRSS.Reader
dotnet run --urls "http://0.0.0.0:5280" &
READER_PID=$!

sleep 2

echo "Iniciando AtrocidadesRSS Generator (porta 5001)..."
cd /home/eduardo/Projects/AtrocidadesRSS/src/AtrocidadesRSS.Generator.Web
dotnet run --urls "http://0.0.0.0:5001" &
GENERATOR_PID=$!

echo ""
echo "✅ Ambos os serviços iniciados!"
echo "📖 Reader:    http://localhost:5280 (PID: $READER_PID)"
echo "📋 Generator: http://localhost:5001 (PID: $GENERATOR_PID)"
echo ""
echo "Pressione Ctrl+C para parar todos os serviços"

trap "kill $READER_PID $GENERATOR_PID 2>/dev/null; echo 'Serviços parados.'; exit" INT TERM

wait