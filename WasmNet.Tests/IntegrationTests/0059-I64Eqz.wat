;; invoke: eqz
;; expect: (i32:0)

(module
    (memory 1)
    (func (export "eqz") (result i32)
        i64.const 4200000000000000000
        i64.eqz
    )
)