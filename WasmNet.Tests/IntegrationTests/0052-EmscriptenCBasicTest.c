// compile to wasm with `emcc ./0052-EmscriptenCBasicTest.c -s EXPORTED_FUNCTIONS=_add`
// then, convert wasm to wat with wasm2wat
int add() {
  int x = 2;
  int y = 3;
  return x + y;
}
