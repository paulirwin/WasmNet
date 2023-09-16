;; invoke: shru
;; expect: (i32:1073741813)

(module
    (func (export "shru") (result i32)
        i32.const -42
        i32.const 2
        i32.shr_u
    )
)