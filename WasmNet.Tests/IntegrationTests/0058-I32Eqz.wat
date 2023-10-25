;; invoke: eqz
;; expect: (i32:0)

(module
    (memory 1)
    (func (export "eqz") (result i32)
        i32.const 42
        i32.eqz
    )
)