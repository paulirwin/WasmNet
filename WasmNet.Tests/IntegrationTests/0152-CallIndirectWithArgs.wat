;; invoke: callByIndex (i32:0)
;; expect: (i32:125)
;; invoke: callByIndex (i32:1)
;; expect: (i32:75)

(module
  (table 2 funcref)
  (func $f1 (param $a i32) (param $b i32) (result i32)
    local.get $a
    local.get $b
    i32.add)
  (func $f2 (param $a i32) (param $b i32) (result i32)
    local.get $a
    local.get $b
    i32.sub)
  (elem (i32.const 0) $f1 $f2)
  (type $binary_i32_func (func (param i32) (param i32) (result i32)))
  (func (export "callByIndex") (param $i i32) (result i32)
    i32.const 100
    i32.const 25
    local.get $i
    call_indirect (type $binary_i32_func))
)